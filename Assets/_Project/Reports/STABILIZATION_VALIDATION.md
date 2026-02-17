# Stabilization Validation (Local)

## Project acceptance checks

1. Run project EditMode tests:
`DormLifeRoguelike.Tests.EditMode`
Expected: all pass.

2. Run project PlayMode tests:
`DormLifeRoguelike.Tests.PlayMode`
Expected: all pass.

3. Regenerate event audit report from:
`Tools > DormLifeRoguelike > Event Audit > Export CSV`
Expected: no `ERROR` rows in `Assets/_Project/Reports/event_audit_report.csv`.

## Known external failures

- Full EditMode run includes package tests from `com.IvanMurzak...` and currently has 3 failing tests.
- These are external to the game namespace and not part of gameplay acceptance.
