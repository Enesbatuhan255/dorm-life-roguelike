param(
    [string]$SourceProjectPath = "",
    [string]$TargetProjectPath = "C:\DormLifeRoguelike\MyProject",
    [switch]$SkipProcessCheck,
    [switch]$SkipOpen,
    [switch]$DryRun
)

$ErrorActionPreference = "Stop"

function Write-Info([string]$Message) { Write-Host "[INFO] $Message" }
function Write-Warn([string]$Message) { Write-Host "[WARN] $Message" }
function Write-Err([string]$Message) { Write-Host "[ERR ] $Message" }

function Resolve-ProjectPath {
    param([string]$PathValue)

    if (-not [string]::IsNullOrWhiteSpace($PathValue)) {
        return (Resolve-Path $PathValue).Path
    }

    return (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}

function Assert-NoRunningUnity {
    $unity = Get-CimInstance Win32_Process -Filter "Name = 'Unity.exe'" -ErrorAction SilentlyContinue
    if ($null -eq $unity) {
        return
    }

    $matched = @()
    foreach ($proc in $unity) {
        if ($proc.CommandLine -and $proc.CommandLine -like "*$SourceResolved*") {
            $matched += $proc
        }
    }

    if ($matched.Count -gt 0) {
        $pids = ($matched | ForEach-Object { $_.ProcessId }) -join ", "
        throw "Unity is running with source project path. Stop Unity first. PID(s): $pids"
    }
}

function Assert-PathPolicy {
    if ($SourceResolved -notmatch "\s") {
        Write-Warn "Source path already has no spaces: $SourceResolved"
    }

    if ($TargetResolved -match "\s") {
        throw "Target path still contains spaces: $TargetResolved"
    }

    if ($SourceResolved -ieq $TargetResolved) {
        throw "Source and target paths are the same."
    }
}

function Copy-Project {
    $targetParent = Split-Path $TargetResolved -Parent

    if (-not (Test-Path $targetParent)) {
        if ($DryRun) {
            Write-Info "DRY RUN: would create target parent folder: $targetParent"
        } else {
            New-Item -ItemType Directory -Path $targetParent -Force | Out-Null
        }
    }

    if (-not (Test-Path $TargetResolved)) {
        if ($DryRun) {
            Write-Info "DRY RUN: would create target folder: $TargetResolved"
        } else {
            New-Item -ItemType Directory -Path $TargetResolved -Force | Out-Null
        }
    }

    $excludeDirs = @("Library", "Temp", "Logs", "Obj")
    $excludeFiles = @("*.csproj", "*.sln", "*.suo", "*.user")
    $cmdParts = @(
        "`"$SourceResolved`"",
        "`"$TargetResolved`"",
        "/E",
        "/R:1",
        "/W:1",
        "/NP",
        "/XD"
    )

    foreach ($dir in $excludeDirs) {
        $cmdParts += "`"$SourceResolved\$dir`""
    }

    $cmdParts += "/XF"
    foreach ($pattern in $excludeFiles) {
        $cmdParts += $pattern
    }

    $cmd = $cmdParts -join " "

    if ($DryRun) {
        Write-Info "DRY RUN: robocopy $cmd"
        return
    }

    Write-Info "Copying project to target (excluding Library/Temp/Logs/Obj)..."
    $rc = Start-Process -FilePath "robocopy.exe" -ArgumentList $cmd -Wait -PassThru -NoNewWindow
    if ($rc.ExitCode -gt 7) {
        throw "robocopy failed with exit code $($rc.ExitCode)"
    }

    Write-Info "Copy completed. robocopy exit code: $($rc.ExitCode)"
}

function Print-NextSteps {
    Write-Host ""
    Write-Info "Migration complete."
    Write-Host "Next steps:"
    Write-Host "1) Open Unity with: `"$TargetResolved`""
    Write-Host "2) Let Unity reimport assets and regenerate Library."
    Write-Host "3) Re-run gates:"
    Write-Host "   - EditMode: DormLifeRoguelike.Tests.EditMode"
    Write-Host "   - PlayMode: DormLifeRoguelike.Tests.PlayMode"
    Write-Host "4) Verify MCP warning about spaces is gone."
    Write-Host "5) If stable, archive old folder: `"$SourceResolved`""
}

$SourceResolved = Resolve-ProjectPath -PathValue $SourceProjectPath
$TargetResolved = $TargetProjectPath

Write-Info "Source: $SourceResolved"
Write-Info "Target: $TargetResolved"

Assert-PathPolicy

if (-not $SkipProcessCheck) {
    Assert-NoRunningUnity
} else {
    Write-Warn "SkipProcessCheck enabled."
}

Copy-Project

if (-not $SkipOpen) {
    if ($DryRun) {
        Write-Info "DRY RUN: would launch Unity Hub target folder."
    } else {
        Start-Process -FilePath "explorer.exe" -ArgumentList "`"$TargetResolved`""
    }
}

Print-NextSteps
