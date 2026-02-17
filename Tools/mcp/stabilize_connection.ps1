param(
    [int]$Port = 54283,
    [switch]$RestartServer,
    [int]$KeepAliveSeconds = 0
)

$ErrorActionPreference = 'Stop'

function Write-Info([string]$msg) { Write-Host "[INFO] $msg" }
function Write-Warn([string]$msg) { Write-Host "[WARN] $msg" }

$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$serverExe = Join-Path $projectRoot 'Library\mcp-server\win-x64\unity-mcp-server.exe'
$baseUrl = "http://localhost:$Port/"

function New-McpSession {
    $initBody = '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"mcp-stabilizer","version":"1.0"}}}'
    $init = Invoke-WebRequest -UseBasicParsing -Uri $baseUrl -Method Post -ContentType 'application/json' -Headers @{ Accept = 'application/json, text/event-stream' } -Body $initBody -TimeoutSec 20

    $session = $init.Headers['Mcp-Session-Id']
    if ([string]::IsNullOrWhiteSpace($session)) {
        throw 'Mcp-Session-Id header is missing from initialize response.'
    }

    $initialized = '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}'
    Invoke-WebRequest -UseBasicParsing -Uri $baseUrl -Method Post -ContentType 'application/json' -Headers @{ Accept = 'application/json, text/event-stream'; 'Mcp-Session-Id' = $session } -Body $initialized -TimeoutSec 20 | Out-Null

    return $session
}

function Invoke-McpRequest {
    param(
        [string]$SessionId,
        [string]$Method,
        [hashtable]$Params,
        [int]$Id = 2
    )

    $payloadObject = @{
        jsonrpc = '2.0'
        id = $Id
        method = $Method
        params = $Params
    }

    $payload = $payloadObject | ConvertTo-Json -Compress -Depth 20
    $response = Invoke-WebRequest -UseBasicParsing -Uri $baseUrl -Method Post -ContentType 'application/json' -Headers @{ Accept = 'application/json, text/event-stream'; 'Mcp-Session-Id' = $SessionId } -Body $payload -TimeoutSec 60

    $jsonLines = ($response.Content -split "`n") | Where-Object { $_ -like 'data:*' } | ForEach-Object { $_.Substring(6) }
    if ($jsonLines.Count -eq 0) {
        throw "No MCP event payload received for method '$Method'."
    }

    ($jsonLines -join '') | ConvertFrom-Json
}

if ($RestartServer) {
    if (-not (Test-Path $serverExe)) {
        throw "Server executable not found: $serverExe"
    }

    $running = Get-Process | Where-Object { $_.ProcessName -eq 'unity-mcp-server' }
    if ($running) {
        Write-Info "Stopping existing unity-mcp-server process(es): $($running.Id -join ', ')"
        $running | Stop-Process -Force
        Start-Sleep -Seconds 1
    }

    Write-Info "Starting unity-mcp-server on port $Port"
    Start-Process -FilePath $serverExe -ArgumentList "port=$Port", 'client-transport=streamableHttp', 'plugin-timeout=300000' -WindowStyle Hidden | Out-Null
    Start-Sleep -Seconds 2
}

$tcpOk = (Test-NetConnection -ComputerName 'localhost' -Port $Port -WarningAction SilentlyContinue).TcpTestSucceeded
if (-not $tcpOk) {
    throw "MCP port $Port is not reachable."
}
Write-Info "Port $Port reachable."

$sessionId = New-McpSession
Write-Info "MCP session initialized. SessionId=$sessionId"

$tools = Invoke-McpRequest -SessionId $sessionId -Method 'tools/list' -Params @{} -Id 3
$toolCount = @($tools.result.tools).Count
Write-Info "tools/list OK. Tool count: $toolCount"

$state = Invoke-McpRequest -SessionId $sessionId -Method 'tools/call' -Params @{ name = 'editor-application-get-state'; arguments = @{} } -Id 4
$editorState = $state.result.structuredContent.result
Write-Info "Editor state: IsPlaying=$($editorState.IsPlaying), IsPaused=$($editorState.IsPaused), IsCompiling=$($editorState.IsCompiling)"

if ($KeepAliveSeconds -gt 0) {
    Write-Info "Keeping MCP session alive for $KeepAliveSeconds second(s)..."
    $stopAt = (Get-Date).AddSeconds($KeepAliveSeconds)
    while ((Get-Date) -lt $stopAt) {
        Start-Sleep -Seconds 10
        [void](Invoke-McpRequest -SessionId $sessionId -Method 'tools/call' -Params @{ name = 'editor-application-get-state'; arguments = @{} } -Id 5)
    }
    Write-Info 'Keep-alive finished.'
} else {
    Write-Warn 'KeepAliveSeconds=0, so indicator may return red when no MCP client stays connected.'
}

Write-Info 'MCP stabilization check completed successfully.'
