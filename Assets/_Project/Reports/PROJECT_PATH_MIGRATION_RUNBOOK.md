# Project Path Migration Runbook (No Spaces)

## Why
- MCP package logs an error when project path contains spaces.
- Current path: `C:\DormLifeRoguelike\My project`
- Target example: `C:\DormLifeRoguelike\MyProject`

## Script
- `Tools/mcp/migrate_project_path_no_spaces.ps1`

## Pre-check
1. Close Unity Editor and any process using the source project.
2. Ensure destination path has no spaces.
3. Keep source project as rollback until validation passes.

## Dry run
```powershell
powershell -ExecutionPolicy Bypass -File ".\Tools\mcp\migrate_project_path_no_spaces.ps1" -DryRun
```

## Execute migration
```powershell
powershell -ExecutionPolicy Bypass -File ".\Tools\mcp\migrate_project_path_no_spaces.ps1" `
  -TargetProjectPath "C:\DormLifeRoguelike\MyProject"
```

## Post-migration validation
1. Open migrated project in Unity.
2. Wait for full reimport.
3. Run gameplay test gates:
   - `DormLifeRoguelike.Tests.EditMode`
   - `DormLifeRoguelike.Tests.PlayMode`
4. Verify MCP no longer logs path-space error.
5. Verify tools:
   - `Tools/mcp/stabilize_connection.ps1`
   - `Tools/mcp/playmode_sanity_check.ps1`

## Rollback
1. Close Unity.
2. Reopen original source project folder.
3. Keep migrated folder for later diff and retry.
