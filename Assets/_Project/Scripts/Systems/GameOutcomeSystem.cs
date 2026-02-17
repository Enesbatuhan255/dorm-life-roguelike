using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class GameOutcomeSystem : IGameOutcomeSystem, IDisposable
    {
        private const string ForcedEndingFlagKey = "forced_ending_id";

        private readonly ITimeManager timeManager;
        private readonly IStatSystem statSystem;
        private readonly GameOutcomeConfig config;
        private readonly AcademicConfig academicConfig;
        private readonly EndingDatabase endingDatabase;
        private readonly IFlagStateService flagStateService;
        private bool isDisposed;
        private int consecutiveCriticalAcademicDays;
        private int consecutiveDebtEnforcementDays;

        public GameOutcomeSystem(ITimeManager timeManager, IStatSystem statSystem, GameOutcomeConfig config, AcademicConfig academicConfig, EndingDatabase endingDatabase)
            : this(timeManager, statSystem, config, academicConfig, endingDatabase, null)
        {
        }

        public GameOutcomeSystem(
            ITimeManager timeManager,
            IStatSystem statSystem,
            GameOutcomeConfig config,
            AcademicConfig academicConfig,
            EndingDatabase endingDatabase,
            IFlagStateService flagStateService)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.academicConfig = academicConfig ?? throw new ArgumentNullException(nameof(academicConfig));
            this.endingDatabase = endingDatabase ?? throw new ArgumentNullException(nameof(endingDatabase));
            this.flagStateService = flagStateService;
            this.timeManager.OnDayChanged += HandleDayChanged;

            CurrentResult = GameOutcomeResult.None;
            consecutiveCriticalAcademicDays = 0;
            consecutiveDebtEnforcementDays = 0;
            EvaluateIfNeeded(this.timeManager.Day);
        }

        public event Action<GameOutcomeResult> OnOutcomeResolved;
        public event Action<GameOutcomeResult> OnGameEnded;

        public bool IsResolved => CurrentResult.IsResolved;

        public GameOutcomeResult CurrentResult { get; private set; }

        public GameOutcomeSnapshot CaptureRuntimeSnapshot()
        {
            return new GameOutcomeSnapshot
            {
                isResolved = CurrentResult.IsResolved,
                consecutiveCriticalAcademicDays = consecutiveCriticalAcademicDays,
                consecutiveDebtEnforcementDays = consecutiveDebtEnforcementDays,
                currentResult = new GameOutcomeResultSnapshot
                {
                    status = (int)CurrentResult.Status,
                    title = CurrentResult.Title,
                    message = CurrentResult.Message,
                    resolvedOnDay = CurrentResult.ResolvedOnDay,
                    score = CurrentResult.Score,
                    scoreBand = CurrentResult.ScoreBand,
                    endingId = (int)CurrentResult.EndingId,
                    epilogTitle = CurrentResult.EpilogTitle,
                    epilogBody = CurrentResult.EpilogBody,
                    debtBand = (int)CurrentResult.DebtBand,
                    employmentState = (int)CurrentResult.EmploymentState
                }
            };
        }

        public void RestoreRuntimeSnapshot(GameOutcomeSnapshot snapshot)
        {
            consecutiveCriticalAcademicDays = Mathf.Max(0, snapshot != null ? snapshot.consecutiveCriticalAcademicDays : 0);
            consecutiveDebtEnforcementDays = Mathf.Max(0, snapshot != null ? snapshot.consecutiveDebtEnforcementDays : 0);

            if (snapshot == null || snapshot.currentResult == null)
            {
                CurrentResult = GameOutcomeResult.None;
                return;
            }

            var restoredStatus = (GameOutcomeStatus)Math.Clamp(snapshot.currentResult.status, (int)GameOutcomeStatus.None, (int)GameOutcomeStatus.Lose);
            if (!snapshot.isResolved || restoredStatus == GameOutcomeStatus.None)
            {
                CurrentResult = GameOutcomeResult.None;
                return;
            }

            var restoredEndingId = Enum.IsDefined(typeof(EndingId), snapshot.currentResult.endingId)
                ? (EndingId)snapshot.currentResult.endingId
                : EndingId.None;
            var restoredDebtBand = Enum.IsDefined(typeof(DebtBand), snapshot.currentResult.debtBand)
                ? (DebtBand)snapshot.currentResult.debtBand
                : DebtBand.None;
            var restoredEmploymentState = Enum.IsDefined(typeof(EmploymentState), snapshot.currentResult.employmentState)
                ? (EmploymentState)snapshot.currentResult.employmentState
                : EmploymentState.Unknown;

            CurrentResult = new GameOutcomeResult(
                restoredStatus,
                snapshot.currentResult.title,
                snapshot.currentResult.message,
                snapshot.currentResult.resolvedOnDay,
                snapshot.currentResult.score,
                snapshot.currentResult.scoreBand,
                restoredEndingId,
                snapshot.currentResult.epilogTitle,
                snapshot.currentResult.epilogBody,
                restoredDebtBand,
                restoredEmploymentState);
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int newDay)
        {
            EvaluateIfNeeded(newDay);
        }

        private void EvaluateIfNeeded(int currentDay)
        {
            if (IsResolved)
            {
                return;
            }

            var academic = statSystem.GetStat(StatType.Academic);
            var money = statSystem.GetStat(StatType.Money);
            if (academic < academicConfig.WarningMin)
            {
                consecutiveCriticalAcademicDays++;
            }
            else
            {
                consecutiveCriticalAcademicDays = 0;
            }

            if (consecutiveCriticalAcademicDays >= academicConfig.CriticalGraceDays)
            {
                ResolveWithScore(currentDay, isEarlyFailure: true);
                return;
            }

            if (money < config.DebtEnforcementThreshold)
            {
                consecutiveDebtEnforcementDays++;
            }
            else
            {
                consecutiveDebtEnforcementDays = 0;
            }

            if (consecutiveDebtEnforcementDays >= config.DebtEnforcementGraceDays)
            {
                ResolveWithScore(currentDay, isEarlyFailure: true, isDebtEnforcementTriggered: true);
                return;
            }

            var campaignEndDay = Math.Max(config.TargetDays, timeManager.TotalDaysInAcademicYear);
            if (currentDay < campaignEndDay)
            {
                return;
            }

            ResolveWithScore(currentDay, isEarlyFailure: false, isDebtEnforcementTriggered: false);
        }

        private void ResolveWithScore(int day, bool isEarlyFailure, bool isDebtEnforcementTriggered = false)
        {
            var academic = statSystem.GetStat(StatType.Academic);
            var mental = statSystem.GetStat(StatType.Mental);
            var energy = statSystem.GetStat(StatType.Energy);
            var money = statSystem.GetStat(StatType.Money);

            var status = isEarlyFailure
                ? GameOutcomeStatus.Lose
                : (academic >= academicConfig.SafeMin ? GameOutcomeStatus.Win : GameOutcomeStatus.Lose);
            var title = status == GameOutcomeStatus.Win ? "PASS" : "FAIL";

            var score = CalculateOutcomeScore();
            if (isEarlyFailure)
            {
                score = Math.Min(score, 64);
            }

            var band = ResolveScoreBand(score);
            var forcedEndingId = TryResolveForcedEndingId();
            var ending = EndingResolver.Resolve(
                isEarlyFailure,
                status == GameOutcomeStatus.Win,
                mental,
                energy,
                money,
                config,
                endingDatabase,
                isDebtEnforcementTriggered,
                forcedEndingId);
            var message = $"{ending.EpilogBody}\nScore: {score}/100 ({band})";

            CurrentResult = new GameOutcomeResult(
                status,
                title,
                message,
                day,
                score,
                band,
                ending.EndingId,
                ending.EpilogTitle,
                ending.EpilogBody,
                ending.DebtBand,
                ending.EmploymentState);
            OnOutcomeResolved?.Invoke(CurrentResult);
            OnGameEnded?.Invoke(CurrentResult);
        }

        private EndingId TryResolveForcedEndingId()
        {
            if (flagStateService == null)
            {
                return EndingId.None;
            }

            if (!flagStateService.TryGetText(ForcedEndingFlagKey, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
            {
                return EndingId.None;
            }

            if (!Enum.TryParse(rawValue.Trim(), true, out EndingId parsed) || parsed == EndingId.None)
            {
                return EndingId.None;
            }

            return parsed;
        }

        private int CalculateOutcomeScore()
        {
            var academic = statSystem.GetStat(StatType.Academic);
            var mental = statSystem.GetStat(StatType.Mental);
            var energy = statSystem.GetStat(StatType.Energy);
            var money = statSystem.GetStat(StatType.Money);

            var academicScore = Mathf.RoundToInt(Mathf.Clamp01(academic / 4f) * 40f);
            var mentalScore = Mathf.RoundToInt(Mathf.Clamp01(mental / 100f) * 20f);
            var energyScore = Mathf.RoundToInt(Mathf.Clamp01((energy + 10f) / 110f) * 15f);
            var moneyScore = ResolveMoneyScore(money);
            var stabilityScore = ResolveStabilityScore(academic, mental, energy, money);

            return academicScore + mentalScore + moneyScore + energyScore + stabilityScore;
        }

        private static int ResolveMoneyScore(float money)
        {
            if (money >= 500f)
            {
                return 15;
            }

            if (money >= 0f)
            {
                return 12;
            }

            if (money >= -500f)
            {
                return 8;
            }

            if (money >= -1500f)
            {
                return 4;
            }

            return 0;
        }

        private static int ResolveStabilityScore(float academic, float mental, float energy, float money)
        {
            var score = 10;

            if (academic < 2f)
            {
                score -= 3;
            }

            if (mental < 40f)
            {
                score -= 3;
            }

            if (energy < 0f)
            {
                score -= 2;
            }

            if (money < -500f)
            {
                score -= 2;
            }

            return Math.Clamp(score, 0, 10);
        }

        private static string ResolveScoreBand(int score)
        {
            if (score >= 85)
            {
                return "Strong Recovery";
            }

            if (score >= 65)
            {
                return "Sustained Under Pressure";
            }

            if (score >= 45)
            {
                return "Extended Year Risk";
            }

            return "System Collapse";
        }
    }
}
