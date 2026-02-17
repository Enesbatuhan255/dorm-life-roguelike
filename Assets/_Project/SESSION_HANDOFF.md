# DormLifeRoguelike - Session Handoff

Last updated: 2026-02-16
Project root: `C:\DormLifeRoguelike\My project`

## Quick Update (2026-02-16 Evening)
- [x] MCP <-> Unity connectivity re-verified via `editor-application-get-state`
- [x] Play mode toggle sanity check done (enter/exit play succeeded)
- [x] UI scene wiring re-audited in `Assets/Scenes/SampleScene.unity`
  - `EventCanvas` present with `EventPanel` + `StatHud`
  - `EventSystem` present with `InputSystemUIInputModule`
  - `EventPanelPresenter` references are wired (panel root, title/description/outcome, 3 choice buttons + labels)
  - `StatHudPresenter`, `ActionPanelPresenter`, `TransactionFeedPresenter`, `DayChangePopupPresenter` present on `StatHud` / `EventCanvas`
- [x] Bootstrap assignments re-checked in scene:
  - `scheduledEvents` contains 3 event assets (`Evt_StudyPush`, `Evt_SleepDebt`, `Evt_PartTimeShift`)
  - `EventCooldownConfig`, `SleepDebtConfig`, `GameOutcomeConfig` are assigned
- [x] Runtime behavior expectations confirmed from scripts:
  - `ActionPanelPresenter` / `TransactionFeedPresenter` / `DayChangePopupPresenter` can self-create missing UI references at runtime
  - `GameBootstrap` auto-attaches `GameOutcomePanelPresenter` if missing in scene
- [x] Current blocker/risk note:
  - Unity MCP warns about project path containing spaces (`C:\DormLifeRoguelike\My project`), which may cause intermittent MCP instability
- [x] EditMode regression snapshot:
  - Project gameplay tests (`DormLifeRoguelike.Tests.EditMode`) passed: 16/16
  - Full editor-wide run showed 3 unrelated package/installer test failures (locale/line-ending sensitive), not in `_Project` gameplay code
- [x] Smoke playtest snapshot (manual MCP, SampleScene):
  - PlayMode sanity pass reached game outcome on Day 8 (`title=PASS`, `resolvedOnDay=8`)
  - No new runtime exceptions in last 5 minutes; only known MCP warning about project path spaces

## Completed (Done)
- [x] Core/service setup: `IService`, `ServiceLocator`, `GameBootstrap`
- [x] Stat system: `IStatSystem`, `StatSystem`, `StatType`, `StatChangedEventArgs`
- [x] Time system: `ITimeManager`, `TimeManager`, `TimeChangedEventArgs`
- [x] Event slice: `IEventManager`, `EventManager`, `EventData`, `EventChoice`, `EventCondition`, `StatEffect`, `ConditionOperator`
- [x] Domain reload safe startup/reset in `GameBootstrap`
- [x] Event flow moved to push + queue (`OnEventStarted`, `OnEventCompleted`, `CurrentEvent`, `HasPendingEvents`, `EnqueueEvent`)
- [x] Event UI presenter + bindings (`EventPanelPresenter`)
- [x] Scene UI created and wired in `SampleScene` (`EventCanvas`, `EventPanel`, choice buttons, outcome text)
- [x] Input System compatibility fixes (`EventInputTrigger`, `InputSystemUiBootstrap`, EventSystem input module updates)
- [x] Stat HUD added and wired (`StatHudPresenter` + 5 stat labels)
- [x] Event apply path validated with runtime logs
- [x] Event scheduler service started (`IEventScheduler`, `EventScheduler`)
- [x] Production event pool created and assigned in `GameBootstrap.scheduledEvents`
- [x] Weighted/random event selection added to `EventScheduler`
- [x] `EventScheduler` now evaluates on day change (`ITimeManager.OnDayChanged`)
- [x] Debug time advance input added (`TimeDebugAdvanceInput`, key: `T`)
- [x] `Bootstrap` scene object now hosts: `GameBootstrap`, `EventDebugRunner`, `EventInputTrigger`, `TimeDebugAdvanceInput`
- [x] Action buttons added (`Study`, `Sleep`, `Work`) with stat changes + `ITimeManager.AdvanceTime` integration
- [x] SleepDebt balancing values moved to `SleepDebtConfig` ScriptableObject and assigned to bootstrap
- [x] SleepDebt service completed (night accumulation + sleep suppression + multipliers)
- [x] Economy service completed (daily costs + action transactions)
- [x] Transaction feed added to StatHud (last 5 entries)
- [x] Day-change popup added (`Yeni Gun`, daily-cost summary)
- [x] Queue policy added in `EventManager` (dedupe + max pending size 5)
- [x] Stable event key flow added (`EventData.eventId`, `OnValidate`, duplicate `scheduledEvents` warning)
- [x] Event Audit Editor tool added (`Run Audit`, `Auto-Fix Missing IDs`, `Export CSV`)
- [x] Missing `eventId` values auto-fixed for existing EventData assets (`EVT_MISC_001..004`)
- [x] Event choices now support optional time advancement (`EventChoice.timeAdvanceHours` -> `ITimeManager.AdvanceTime`)
- [x] Mini win/lose condition added (`IGameOutcomeSystem`, `GameOutcomeSystem`, `GameOutcomeResult`)
- [x] Outcome rules moved to `GameOutcomeConfig` (target day, pass/fail thresholds, fail priority)
- [x] Canonical outcome asset created and assigned in scene (`Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset`)
- [x] GameOver/Pass panel added and auto-attached (`GameOutcomePanelPresenter`)
- [x] Player actions now no-op after outcome resolution (`PlayerActionService` guard)
- [x] Regression tests added for outcome flow (`OnGameEnded` single-fire, resolved-after-action no-op)
- [x] MCP PlayMode sanity check script added (`Tools/mcp/playmode_sanity_check.ps1`)
- [x] Scheduler/Queue regression tests expanded (`EventManagerQueuePolicyTests`, `EventSchedulerPolicyTests`)
- [x] Event cooldown balancing moved to config asset (`EventCooldownConfig`: default + per-category + per-event override)
- [x] `EventScheduler` wired to `EventCooldownConfig` (event override > category override > default)
- [x] `GameBootstrap` wired with `eventCooldownConfig` inspector field + runtime fallback warning
- [x] Canonical cooldown asset created and assigned in scene (`Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset`)
- [x] `EventData` category field added and exposed for cooldown/category-based logic

## In Progress
- [ ] Run full EditMode regression once with stable runner (MCP was flaky; Unity batch blocked while editor instance is open)

## Next (Planned)
- [ ] Add event category filters (time/stat context aware)
- [ ] Add event chain/follow-up support
- [ ] Polish UI/UX for Event panel and Stat HUD

## Where To Continue Tomorrow
1. Close Unity Editor and run full EditMode regression (or run via stable MCP session) to capture fresh XML results.
2. Verify new scheduler coverage includes 6 tests in `EventSchedulerPolicyTests` (category/event override scenarios).
3. Add event category filters and chain/follow-up support.
4. Tune cooldown/category/queue policy based on playtest.

## Important Changed Files
- `Assets/_Project/Scripts/Core/GameBootstrap.cs`
- `Assets/_Project/Scripts/Events/EventChoice.cs`
- `Assets/_Project/Scripts/Systems/IEventManager.cs`
- `Assets/_Project/Scripts/Systems/EventManager.cs`
- `Assets/_Project/Scripts/Systems/IEventScheduler.cs`
- `Assets/_Project/Scripts/Systems/EventScheduler.cs`
- `Assets/_Project/Scripts/UI/EventPanelPresenter.cs`
- `Assets/_Project/Scripts/UI/StatHudPresenter.cs`
- `Assets/_Project/Scripts/UI/ActionPanelPresenter.cs`
- `Assets/_Project/Scripts/UI/TransactionFeedPresenter.cs`
- `Assets/_Project/Scripts/UI/DayChangePopupPresenter.cs`
- `Assets/_Project/Scripts/UI/GameOutcomePanelPresenter.cs`
- `Assets/_Project/Scripts/Utilities/EventDebugRunner.cs`
- `Assets/_Project/Scripts/Utilities/EventInputTrigger.cs`
- `Assets/_Project/Scripts/Utilities/TimeDebugAdvanceInput.cs`
- `Assets/_Project/Scripts/Utilities/InputSystemUiBootstrap.cs`
- `Assets/_Project/Scripts/Editor/EventAuditWindow.cs`
- `Assets/_Project/Reports/event_audit_report.csv`
- `Assets/Scenes/SampleScene.unity`
- `Assets/_Project/Scripts/Systems/IGameOutcomeSystem.cs`
- `Assets/_Project/Scripts/Systems/GameOutcomeResult.cs`
- `Assets/_Project/Scripts/Systems/GameOutcomeSystem.cs`
- `Assets/_Project/Scripts/Systems/GameOutcomeConfig.cs`
- `Assets/_Project/Scripts/Systems/EventCooldownConfig.cs`
- `Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset`
- `Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset`
- `Assets/_Project/Tests/Editor/GameOutcomeSystemTests.cs`
- `Assets/_Project/Tests/Editor/EventSchedulerPolicyTests.cs`
- `Assets/_Project/Tests/Editor/EventManagerQueuePolicyTests.cs`
- `Tools/mcp/playmode_sanity_check.ps1`
- `Assets/_Project/PROJECT_TASK_CHECKLIST.md`
