# Stability Baseline - 2026-02-18

## Scope
- Gameplay-only acceptance scope (`DormLifeRoguelike.Tests.*`)
- Production event content under `Assets/_Project/ScriptableObjects/Events`

## Validation snapshot
- EditMode (`DormLifeRoguelike.Tests.EditMode`): Passed 76/76
- PlayMode (`DormLifeRoguelike.Tests.PlayMode`): Passed 1/1
- Choice coverage report: 0 violations
- Event chain graph report: all exported targets resolve (`targetExists=1`)

## Notes
- Full editor-wide run still contains known external package failures in `com.IvanMurzak...`.
- These external package failures are excluded from gameplay acceptance.
