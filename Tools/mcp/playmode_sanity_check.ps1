$ErrorActionPreference = "Stop"

function New-McpSession {
    $headers = @{
        "Accept" = "application/json, text/event-stream"
        "Content-Type" = "application/json"
    }

    $initPayload = '{"jsonrpc":"2.0","id":"1","method":"initialize","params":{"protocolVersion":"2025-06-18","capabilities":{},"clientInfo":{"name":"codex-sanity","version":"1.0"}}}'
    $initResponse = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:54283" -Headers $headers -Method Post -Body $initPayload -TimeoutSec 20
    $headers["Mcp-Session-Id"] = $initResponse.Headers["Mcp-Session-Id"]

    $initializedPayload = '{"jsonrpc":"2.0","method":"notifications/initialized","params":{}}'
    Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:54283" -Headers $headers -Method Post -Body $initializedPayload -TimeoutSec 20 | Out-Null
    return $headers
}

function Invoke-McpTool {
    param(
        [hashtable]$Headers,
        [string]$ToolName,
        [hashtable]$Arguments
    )

    $payloadObject = @{
        jsonrpc = "2.0"
        id = [guid]::NewGuid().ToString("N")
        method = "tools/call"
        params = @{
            name = $ToolName
            arguments = $Arguments
        }
    }

    $payload = $payloadObject | ConvertTo-Json -Compress -Depth 40
    $response = Invoke-WebRequest -UseBasicParsing -Uri "http://localhost:54283" -Headers $Headers -Method Post -Body $payload -TimeoutSec 120
    $dataLines = ($response.Content -split "`n") | Where-Object { $_ -like "data:*" } | ForEach-Object { $_.Substring(6) }
    $json = ($dataLines -join "")
    if ([string]::IsNullOrWhiteSpace($json)) {
        return $null
    }

    return $json | ConvertFrom-Json
}

function Get-PropertyValue {
    param(
        $Props,
        [string]$Name
    )

    $entry = $Props | Where-Object { $_.name -eq $Name } | Select-Object -First 1
    if ($null -eq $entry) {
        return $null
    }

    return $entry.value
}

$headers = New-McpSession

try {
    Invoke-McpTool -Headers $headers -ToolName "assets-refresh" -Arguments @{} | Out-Null

    $sceneSearch = Invoke-McpTool -Headers $headers -ToolName "assets-find" -Arguments @{
        filter = "t:Scene SampleScene"
        searchInFolders = @("Assets/Scenes")
        maxResults = 5
    }

    $sceneRef = $sceneSearch.result.structuredContent.result[0]
    if ($null -eq $sceneRef) {
        throw "SampleScene asset not found."
    }

    Invoke-McpTool -Headers $headers -ToolName "scene-open" -Arguments @{ sceneRef = $sceneRef; loadSceneMode = "Single" } | Out-Null
    Invoke-McpTool -Headers $headers -ToolName "scene-set-active" -Arguments @{ sceneRef = $sceneRef } | Out-Null
    Invoke-McpTool -Headers $headers -ToolName "editor-application-set-state" -Arguments @{ isPlaying = $true } | Out-Null
    Start-Sleep -Seconds 2

    $script = @'
using DormLifeRoguelike;
using UnityEngine;

public static class Script
{
    public static object Main()
    {
        if (!ServiceLocator.TryGet<IStatSystem>(out var stats) ||
            !ServiceLocator.TryGet<ITimeManager>(out var time) ||
            !ServiceLocator.TryGet<IGameOutcomeSystem>(out var outcome))
        {
            return new { ok = false, error = "Required services not found" };
        }

        stats.SetBaseValue(StatType.Academic, 85f);
        stats.SetBaseValue(StatType.Mental, 80f);
        stats.SetBaseValue(StatType.Money, 0f);

        var guard = 0;
        while (!outcome.IsResolved && time.Day <= 8 && guard < 32)
        {
            time.AdvanceTime(24);
            guard++;
        }

        var result = outcome.CurrentResult;
        return new
        {
            ok = true,
            resolved = outcome.IsResolved,
            resolvedOnDay = result.ResolvedOnDay,
            title = result.Title,
            day = time.Day
        };
    }
}
'@

    $execResult = Invoke-McpTool -Headers $headers -ToolName "script-execute" -Arguments @{
        csharpCode = $script
        className = "Script"
        methodName = "Main"
        parameters = @()
    }

    $props = $execResult.result.structuredContent.result.props
    $ok = Get-PropertyValue -Props $props -Name "ok"
    $resolved = Get-PropertyValue -Props $props -Name "resolved"
    $resolvedOnDay = Get-PropertyValue -Props $props -Name "resolvedOnDay"
    $title = Get-PropertyValue -Props $props -Name "title"

    if (-not $ok) {
        throw "Sanity script returned ok=false."
    }

    if (-not $resolved) {
        throw "Sanity failed: outcome not resolved."
    }

    if ([int]$resolvedOnDay -ne 8) {
        throw "Sanity failed: expected resolvedOnDay=8, got $resolvedOnDay."
    }

    if ([string]::IsNullOrWhiteSpace([string]$title)) {
        throw "Sanity failed: title is empty."
    }

    Write-Output "PASS: PlayMode sanity check passed (resolvedOnDay=$resolvedOnDay, title=$title)."
    exit 0
}
finally {
    try {
        Invoke-McpTool -Headers $headers -ToolName "editor-application-set-state" -Arguments @{ isPlaying = $false } | Out-Null
    }
    catch {
        Write-Warning "Could not force stop PlayMode: $($_.Exception.Message)"
    }
}
