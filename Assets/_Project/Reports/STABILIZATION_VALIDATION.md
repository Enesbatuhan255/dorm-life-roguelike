# Stabilization Validation (Local)

## Project acceptance checks

1. Run gameplay EditMode gate (namespace-scoped):
`DormLifeRoguelike.Tests.EditMode`
Expected: all pass.

2. Run gameplay PlayMode gate (namespace-scoped):
`DormLifeRoguelike.Tests.PlayMode`
Expected: all pass.

3. Regenerate event audit report from:
`Tools > DormLifeRoguelike > Event Audit > Export CSV`
Expected: no `ERROR` rows in `Assets/_Project/Reports/event_audit_report.csv`.

4. Regenerate event content reports:
`Tools > DormLifeRoguelike > Validate Choice Coverage`
`Tools > DormLifeRoguelike > Export Event Chain Graph`
Expected:
- `Assets/_Project/Reports/event_choice_coverage_report.csv` has 0 `VIOLATION`.
- `Assets/_Project/Reports/event_chain_graph.csv` has no broken targets (`targetExists=0`).

## Baseline lock (2026-02-18)

- Gameplay EditMode gate (`DormLifeRoguelike.Tests.EditMode`): Passed 76/76
- Gameplay PlayMode gate (`DormLifeRoguelike.Tests.PlayMode`): Passed 1/1
- Choice coverage report: generated, 0 violations
- Event chain graph report: generated, all exported targets resolve

## Known external failures

- Full EditMode run includes package tests from `com.IvanMurzak...` and currently has 3 failing tests.
- These are external to the game namespace and not part of gameplay acceptance.
