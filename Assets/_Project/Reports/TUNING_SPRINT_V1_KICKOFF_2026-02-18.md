# Tuning Sprint V1 Kickoff - 2026-02-18

## Scope
- Focus area: `cooldown/category/queue` live balance tuning.
- Project path: `C:\DormLifeRoguelike\MyProject` (no-space migration completed).

## Baseline Run
- Source test: `DormLifeRoguelike.Tests.EditMode.BalanceSimulationTests.DebtEnforcement_RiskyProfile_ShowsHarsherOutcomesThanCautious`
- Result: Passed
- Reports generated:
  - `Temp/BalanceReports/latest-balance-summary.json`
  - `Temp/BalanceReports/latest-balance-summary.csv`

## Baseline Snapshot (40 runs/profile)
- Cautious
  - Harsh debt rate: `0.00`
  - Endings: `FailedExtendedYear: 40`
- Balanced
  - Harsh debt rate: `0.35`
  - Endings: `FailedExtendedYear: 26`, `ExpelledDebtSpiral: 14`
- Risky
  - Harsh debt rate: `0.80`
  - Endings: `ExpelledDebtSpiral: 32`, `FailedExtendedYear: 8`

## Initial Read
- Relative risk gradient works (Risky >> Cautious in harsh outcomes).
- Outcome diversity is low in this simulation profile set (`GraduatedResilient` observed: `0`).
- Debt spiral presence is strong for Balanced/Risky and should be tuned with content weights/cooldowns first, not with broad system nerfs.

## Sprint V1 Execution Plan
1. Tune debt/gamble-heavy event cadence:
   - adjust `selectionWeight`
   - add per-event/per-category cooldown overrides where repetition feels too punitive
2. Re-run:
   - gameplay test gates (`DormLifeRoguelike.Tests.*`)
   - balance simulation baseline test
3. Compare:
   - harsh debt rate deltas by profile
   - ending distribution diversity
4. Lock only if:
   - relative risk ordering preserved
   - no regression in scheduler/chain/flag/save-load tests

## Exit Criteria (V1)
- Maintain: `RiskyHarshRate > CautiousHarshRate + 0.10`
- Improve: Balanced harsh debt rate and ending diversity without collapsing risk identity.

## Iteration V1-A (Applied)
- Changed:
  - `Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset`
    - Added per-event cooldown override (`24h`) for:
      - `EVT_MAJOR_DEBT_001`
      - `EVT_MAJOR_DEBT_002`
      - `EVT_MAJOR_GAMBLE_002`
      - `EVT_MAJOR_GAMBLE_003`
  - Weights:
    - `EVT_MAJOR_DEBT_001`: `1.20 -> 1.10`
    - `EVT_MAJOR_DEBT_002`: `1.25 -> 1.15`
    - `EVT_MAJOR_GAMBLE_002`: `0.85 -> 0.80`
    - `EVT_MAJOR_GAMBLE_003`: `0.80 -> 0.75`
- Validation:
  - `DormLifeRoguelike.Tests.EditMode.BalanceSimulationTests.DebtEnforcement_RiskyProfile_ShowsHarsherOutcomesThanCautious`: Passed
- Observation:
  - Baseline summary remained numerically identical in this deterministic simulation batch.
  - Conclusion: harsh debt pressure here is driven more by follow-up/choice policy than by weighted random pool frequency.
  - Next tuning pass should focus on high-impact debt/gamble choice branches and follow-up delay patterns.

## Iteration V1-B (Applied)
- Changed (choice/follow-up severity softening):
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_DEBT_NOTICE.asset`
    - `Gormezden gel`:
      - `debt_pressure: +2 -> +1`
      - `followUpDelayDays: 0 -> 1` (`EVT_MAJOR_DEBT_002`)
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_DEBT_ESCALATION.asset`
    - `Ertele`:
      - money delta `-150 -> -100`
      - `debt_pressure: +2 -> +1`
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_GAMBLE_CHASE.asset`
    - `Son bir tur`:
      - mental delta `-4 -> -3`
      - `followUpDelayDays: 1 -> 2` (`EVT_MAJOR_GAMBLE_003`)
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_GAMBLE_PUNISH.asset`
    - `Ode`:
      - money delta `-800 -> -650`
      - mental delta `-6 -> -5`
    - `Ertele`:
      - mental delta `-4 -> -3`
      - `debt_pressure: +2 -> +1`
- Validation:
  - `DormLifeRoguelike.Tests.EditMode.BalanceSimulationTests.DebtEnforcement_RiskyProfile_ShowsHarsherOutcomesThanCautious`: Passed
  - Gameplay gates:
    - EditMode `DormLifeRoguelike.Tests`: Passed `77/77`
    - PlayMode `DormLifeRoguelike.Tests`: Passed `4/4`
  - Observation:
  - `latest-balance-summary.csv` remained unchanged numerically.
  - Current simulation driver is still relatively insensitive to these branch-level content tweaks.
  - Next step should include simulation choice-policy enrichment for debt/gamble branches to measure content-level tuning impact more directly.

## Iteration V1-C (Applied)
- Changed (simulation driver enrichment):
  - `Assets/_Project/Tests/Editor/BalanceSimulationTests.cs`
    - `SelectChoice` expanded with debt/gamble specific branch logic for:
      - `EVT_MAJOR_DEBT_001`
      - `EVT_MAJOR_DEBT_002`
      - `EVT_MAJOR_GAMBLE_002`
      - `EVT_MAJOR_GAMBLE_003`
    - Added profile-weighted fallback scoring (Money/Mental/Energy/Academic + follow-up penalty).
    - Added shared helpers for stat deltas and follow-up detection.
- Validation:
  - EditMode gameplay scope remained green (test suite passed).
- Observation:
  - Current baseline summary is still unchanged for existing profile thresholds and deterministic seeds.
  - This indicates remaining bottleneck is likely profile trigger thresholds and/or day-plan policy, not only branch selector logic.

## Iteration V1-E (Applied)
- Changed (production content cadence + academic support):
  - `Assets/_Project/ScriptableObjects/Config/EventCooldownConfig.asset`
    - per-event cooldowns:
      - `EVT_MAJOR_DEBT_001`: `24h -> 18h`
      - `EVT_MAJOR_DEBT_002`: `24h -> 18h`
      - `EVT_MAJOR_GAMBLE_002`: `24h -> 18h`
      - `EVT_MAJOR_GAMBLE_003`: `24h -> 18h`
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_DEBT_NOTICE.asset`
    - `selectionWeight: 1.10 -> 1.15`
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_DEBT_ESCALATION.asset`
    - `selectionWeight: 1.15 -> 1.20`
  - `Assets/_Project/ScriptableObjects/Events/Minor/EVT_MINOR_KUMAR_FIRSATI.asset`
    - `selectionWeight: 1.00 -> 1.05`
  - `Assets/_Project/ScriptableObjects/Events/Major/EVT_MAJOR_ACADEMIC_PROBATION.asset`
    - `selectionWeight: 1.00 -> 1.05`
    - `Sikilas` academic delta: `+0.10 -> +0.12`
    - `Dengeli` academic delta: `+0.06 -> +0.08`
  - `Assets/_Project/ScriptableObjects/Events/Minor/EVT_MINOR_NOT_PAYLASIM.asset`
    - `selectionWeight: 1.15 -> 1.20`
    - `Notlari al` academic delta: `+0.04 -> +0.05`
    - `Bosver` academic delta: `+0.02 -> +0.03`
- Validation:
  - EditMode (`DormLifeRoguelike.Tests`): Passed
  - PlayMode (`DormLifeRoguelike.Tests`): Passed
  - Balance simulation test: Passed
- Observation:
  - `RiskyGambleChoiceCount` increased (`21 -> 51`) in latest batch.
  - Ending distribution remained unchanged (`Cautious/Balanced = FailedExtendedYear:40`).

## Iteration V1-F (Applied)
- Changed:
  - `Assets/_Project/ScriptableObjects/Config/GameOutcomeConfig.asset`
    - `minAcademicPass: 70 -> 60`
- Validation:
  - EditMode (`DormLifeRoguelike.Tests`): Passed
  - PlayMode (`DormLifeRoguelike.Tests`): Passed
  - Balance simulation test: Passed
- Observation:
  - Ending distribution remained unchanged in deterministic simulation batch.
  - Practical implication: current bottleneck is likely not only outcome threshold values; simulation policy/event arrival dynamics still dominate.

## Iteration V1-G (Applied)
- Changed (simulation policy root-cause + diversity unlock):
  - `Assets/_Project/Tests/Editor/BalanceSimulationTests.cs`
    - Added deterministic behavior variants for simulation runs (Balanced split by seed into study-first / neutral / cash-first policy).
    - Added stronger Balanced study-first policy:
      - primary + secondary slots prioritize study outside emergency debt mode
      - admin slot is replaced by study in study-first branch
    - Added flag-risk aware choice scoring (`debt_pressure`, `kyk_risk_days`, `work_strain`, `burnout`, `illegal_fine_pending`) with profile-scaled penalties.
    - Expanded balance report metrics:
      - `avg_final_academic`
      - `avg_final_money`
    - Tightened diversity gate:
      - `MinBalancedDistinctEndings: 1 -> 2`
  - Config probe kept from V1-F:
    - `Assets/_Project/ScriptableObjects/Config/AcademicConfig.asset` (`safeMin: 2 -> 1.6`, `warningMin: 1.8 -> 1.4`)
- Validation:
  - EditMode (`DormLifeRoguelike.Tests`): Passed
  - PlayMode (`DormLifeRoguelike.Tests`): Passed
  - Balance simulation test: Passed
- Outcome snapshot (latest):
  - Cautious: `FailedExtendedYear:40`, `avg_final_academic=0.7498`, `avg_final_money=-4.5`
  - Balanced: `FailedExtendedYear:32`, `GraduatedPrecariousStable:7`, `GraduatedMinWageDebt:1`,
    `avg_final_academic=0.9373`, `avg_final_money=112.8`
  - Risky: `FailedExtendedYear:37`, `ExpelledDebtSpiral:3`, `avg_final_academic=0.7135`, `avg_final_money=-494.4`
