# DormLifeRoguelike - Project Checklist

## Completed
- [x] Core service layer (`IService`, `ServiceLocator`, `GameBootstrap`)
- [x] Stat system (`IStatSystem`, `StatSystem`, stat events/types)
- [x] Time system (`ITimeManager`, `TimeManager`, time events)
- [x] Event vertical slice (`IEventManager`, `EventManager`, event data models)
- [x] Domain reload safe startup/reset in bootstrap
- [x] Event debug runner added (`EventDebugRunner`)
- [x] Event flow converted to push + queue model
- [x] Event UI presenter implemented (`EventPanelPresenter`)
- [x] Scene UI wired (`EventCanvas`, `EventPanel`, choice buttons, outcome text)
- [x] Input System compatibility fixes for UI/Event trigger
- [x] Stat HUD presenter added and wired (`StatHudPresenter`)
- [x] Choice apply path validated with runtime logs
- [x] Event scheduler service started (`IEventScheduler`, `EventScheduler`)
- [x] Production event pool assets created and assigned to `GameBootstrap.scheduledEvents`
- [x] Weighted/random event selection implemented in scheduler
- [x] Scheduler checks on day change (`ITimeManager.OnDayChanged`)
- [x] Action buttons (`Study`, `Sleep`, `Work`) added with stat + time effects
- [x] SleepDebt values moved to `SleepDebtConfig` ScriptableObject and wired in bootstrap
- [x] SleepDebt system integrated (night accumulation, sleep suppression, action multipliers)
- [x] Economy MVP integrated (`IEconomySystem`, daily costs, action transactions)
- [x] Transaction feed UI added (last 5 economy transactions)
- [x] Day-change popup added with daily-cost summary
- [x] Queue policy added (dedupe + max pending size = 5)
- [x] Event stable ID flow added (`eventId`, OnValidate checks, duplicate scheduled ID warning)
- [x] Event Audit tool added (audit + auto-fix missing IDs + CSV export)
- [x] Mini win/lose condition system added (`IGameOutcomeSystem`, `GameOutcomeSystem`, day-based evaluation)
- [x] Outcome flow moved to config (`GameOutcomeConfig` ScriptableObject + fail priority)
- [x] Canonical outcome config asset created and assigned (`Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset`)
- [x] GameOver/Pass full-screen panel added (`GameOutcomePanelPresenter`)
- [x] Action pipeline hardened after resolve (actions are no-op when game outcome is resolved)
- [x] Regression tests added (`OnGameEnded` single-fire + resolved-after-action no-op)
- [x] PlayMode sanity script added (`Tools/mcp/playmode_sanity_check.ps1`)
- [x] Scheduler/Queue regression tests expanded (active same-ID dedupe, shared cooldown key, all-ineligible no-queue)

## In Progress
- [ ] Hook additional gameplay time progression into scheduler (currently actions + debug)

## Next
- [ ] Add non-debug time source (sleep/actions/clock loop) to call `ITimeManager.AdvanceTime`
- [ ] Add event category filters
- [ ] Add per-event cooldown tuning asset/config (instead of global cooldown only)
- [ ] Add event chain support (follow-up event IDs)
- [ ] Add Stat HUD polish (formatting/colors/icons)
- [ ] Add playmode/editmode tests for scheduler, economy, sleep-debt and queue behavior
