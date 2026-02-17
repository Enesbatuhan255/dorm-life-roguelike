using System;

namespace DormLifeRoguelike
{
    public sealed class PlayerActionService : IPlayerActionService, IDisposable
    {
        private const float StudyPerfectAcademicBonus = 0.08f;
        private const float StudyPoorAcademicPenalty = -0.05f;
        private const float StudyPoorMentalPenalty = -1f;
        private const float WorkPerfectMoneyBonus = 15f;
        private const float WorkPoorMoneyPenalty = -10f;
        private const float WorkPoorMentalPenalty = -1f;
        private const float AdminPerfectMoneyBonus = 5f;
        private const float AdminPoorMoneyPenalty = -10f;
        private const float AdminPoorMentalPenalty = -1f;

        private readonly IStatSystem statSystem;
        private readonly ITimeManager timeManager;
        private readonly ISleepDebtSystem sleepDebtSystem;
        private readonly IEconomySystem economySystem;
        private readonly IGameOutcomeSystem gameOutcomeSystem;
        private readonly IEventScheduler eventScheduler;
        private readonly IEventManager eventManager;
        private readonly IWorkLimitSystem workLimitSystem;
        private readonly MentalConfig mentalConfig;
        private readonly IInflationShockSystem inflationShockSystem;

        private bool isDisposed;
        private bool isEndDayPending;
        private float lowestEnergyThisDay;
        private int energyPenaltyDaysRemaining;

        public PlayerActionService(
            IStatSystem statSystem,
            ITimeManager timeManager,
            ISleepDebtSystem sleepDebtSystem,
            IEconomySystem economySystem,
            IGameOutcomeSystem gameOutcomeSystem)
            : this(
                statSystem,
                timeManager,
                sleepDebtSystem,
                economySystem,
                gameOutcomeSystem,
                null,
                null,
                null,
                MentalConfig.CreateRuntimeDefault(),
                null)
        {
        }

        public PlayerActionService(
            IStatSystem statSystem,
            ITimeManager timeManager,
            ISleepDebtSystem sleepDebtSystem,
            IEconomySystem economySystem,
            IGameOutcomeSystem gameOutcomeSystem,
            IEventScheduler eventScheduler,
            IEventManager eventManager,
            IWorkLimitSystem workLimitSystem,
            MentalConfig mentalConfig,
            IInflationShockSystem inflationShockSystem)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.sleepDebtSystem = sleepDebtSystem ?? throw new ArgumentNullException(nameof(sleepDebtSystem));
            this.economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));
            this.gameOutcomeSystem = gameOutcomeSystem ?? throw new ArgumentNullException(nameof(gameOutcomeSystem));
            this.eventScheduler = eventScheduler;
            this.eventManager = eventManager;
            this.workLimitSystem = workLimitSystem;
            this.mentalConfig = mentalConfig ?? MentalConfig.CreateRuntimeDefault();
            this.inflationShockSystem = inflationShockSystem;

            lowestEnergyThisDay = this.statSystem.GetStat(StatType.Energy);
            energyPenaltyDaysRemaining = 0;

            this.timeManager.OnDayChanged += HandleDayChanged;
            if (this.eventManager != null)
            {
                this.eventManager.OnEventCompleted += HandleEventCompleted;
            }
        }

        public int RemainingWorkActionsThisWeek => workLimitSystem?.RemainingWorkActionsThisWeek ?? int.MaxValue;

        public bool CanWorkThisWeek => workLimitSystem == null || workLimitSystem.CanWork();

        public void ApplyStudy(int hours)
        {
            ApplyStudy(hours, MicroChallengeOutcomeBand.Good);
        }

        public void ApplyStudy(int hours, MicroChallengeOutcomeBand outcomeBand)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            statSystem.ApplyBaseDelta(StatType.Academic, 0.2f);

            var currentMental = statSystem.GetStat(StatType.Mental);
            var sleepScaledEnergyCost = ScaleNegativeCost(-12f, sleepDebtSystem.GetEnergyCostMultiplier());
            var studyEnergyDelta = StudyCostCalculator.CalculateEnergyDelta(sleepScaledEnergyCost, currentMental, mentalConfig);
            ApplyEnergyDelta(studyEnergyDelta);

            statSystem.ApplyBaseDelta(StatType.Mental, ScaleNegativeCost(-5f, sleepDebtSystem.GetMentalCostMultiplier()));
            statSystem.ApplyBaseDelta(StatType.Hunger, -8f);
            if (UnityEngine.Random.value < 0.5f)
            {
                economySystem.ApplyTransaction(ApplyInflationToExpense(-5f), "Coffee/Printing");
            }

            timeManager.AdvanceTime(hours);
            ApplyStudyOutcome(outcomeBand);
        }

        public void ApplySleep(int hours)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            sleepDebtSystem.BeginSleep(hours);
            try
            {
                sleepDebtSystem.ApplySleepAction(hours);
                statSystem.ApplyBaseDelta(StatType.Hunger, -15f);
                timeManager.AdvanceTime(hours);
            }
            finally
            {
                sleepDebtSystem.EndSleep();
            }

            UpdateLowestEnergy(statSystem.GetStat(StatType.Energy));
        }

        public void ApplyWork(int hours)
        {
            ApplyWork(hours, MicroChallengeOutcomeBand.Good);
        }

        public void ApplyWork(int hours, MicroChallengeOutcomeBand outcomeBand)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            if (workLimitSystem != null && !workLimitSystem.TryConsumeWorkAction())
            {
                economySystem.ApplyTransaction(0f, "Work limit reached this week");
                return;
            }

            economySystem.ApplyTransaction(80f, "Part-time shift");
            ApplyEnergyDelta(ScaleNegativeCost(-15f, sleepDebtSystem.GetEnergyCostMultiplier()));
            statSystem.ApplyBaseDelta(StatType.Mental, ScaleNegativeCost(-4f, sleepDebtSystem.GetMentalCostMultiplier()));
            statSystem.ApplyBaseDelta(StatType.Hunger, -10f);
            ApplyWorkOutcome(outcomeBand);

            timeManager.AdvanceTime(hours);
        }

        public void ApplyWait(int hours)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            statSystem.ApplyBaseDelta(StatType.Hunger, -2f * hours);
            ApplyEnergyDelta(ScaleNegativeCost(-1f * hours, sleepDebtSystem.GetEnergyCostMultiplier()));
            statSystem.ApplyBaseDelta(StatType.Mental, 0.5f * hours);
            timeManager.AdvanceTime(hours);
        }

        public void ApplyAdmin(int hours)
        {
            ApplyAdmin(hours, MicroChallengeOutcomeBand.Good);
        }

        public void ApplyAdmin(int hours, MicroChallengeOutcomeBand outcomeBand)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            // Admin consumes time/focus similarly to waiting but does not grant passive mental recovery.
            statSystem.ApplyBaseDelta(StatType.Hunger, -2f * hours);
            ApplyEnergyDelta(ScaleNegativeCost(-1f * hours, sleepDebtSystem.GetEnergyCostMultiplier()));
            timeManager.AdvanceTime(hours);
            ApplyAdminOutcome(outcomeBand);
        }

        public void ApplySocialize(int hours)
        {
            if (!CanApplyAction(hours))
            {
                return;
            }

            var mental = statSystem.GetStat(StatType.Mental);
            var gain = mental < mentalConfig.SocializeLowMentalThreshold
                ? mentalConfig.SocializeLowMentalGain
                : mentalConfig.SocializeMentalGain;

            ApplyEnergyDelta(mentalConfig.SocializeEnergyCost);
            statSystem.ApplyBaseDelta(StatType.Mental, gain);
            statSystem.ApplyBaseDelta(StatType.Hunger, -1f * hours);
            timeManager.AdvanceTime(hours);
        }

        public bool TryEndDay(out string message)
        {
            message = string.Empty;
            if (gameOutcomeSystem.IsResolved)
            {
                message = "Game is already resolved.";
                return false;
            }

            if (isEndDayPending)
            {
                message = "Day end is already in progress.";
                return false;
            }

            if (eventManager != null && (eventManager.CurrentEvent != null || eventManager.HasPendingEvents))
            {
                message = "Resolve active event first.";
                return false;
            }

            var hasMajor = eventScheduler != null && eventScheduler.TryQueueMajorForCurrentDay();
            if (hasMajor)
            {
                isEndDayPending = true;
                message = "Major event started. Resolve it to close the day.";
                return true;
            }

            AdvanceToNextDay();
            message = "No major event. Day ended.";
            return true;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
            if (eventManager != null)
            {
                eventManager.OnEventCompleted -= HandleEventCompleted;
            }
        }

        private void HandleDayChanged(int _)
        {
            ApplyOverdriveOutcomeFromPreviousDay();
            ApplyDailySevereEnergyPenalty();
            lowestEnergyThisDay = statSystem.GetStat(StatType.Energy);
            isEndDayPending = false;
        }

        private void HandleEventCompleted(EventData eventData)
        {
            if (!isEndDayPending)
            {
                return;
            }

            if (eventData == null || !IsMajorCategory(eventData.Category))
            {
                return;
            }

            AdvanceToNextDay();
            isEndDayPending = false;
        }

        private void AdvanceToNextDay()
        {
            var hoursRemaining = 24 - Math.Clamp(timeManager.Hour, 0, 23);
            if (hoursRemaining <= 0)
            {
                hoursRemaining = 24;
            }

            timeManager.AdvanceTime(hoursRemaining);
        }

        private void ApplyOverdriveOutcomeFromPreviousDay()
        {
            if (lowestEnergyThisDay >= 0f)
            {
                return;
            }

            if (lowestEnergyThisDay >= mentalConfig.MildOverdriveMin)
            {
                statSystem.ApplyBaseDelta(StatType.Mental, mentalConfig.MildOverdriveMentalPenalty);
                return;
            }

            statSystem.ApplyBaseDelta(StatType.Mental, mentalConfig.SevereOverdriveMentalPenalty);
            energyPenaltyDaysRemaining = Math.Max(energyPenaltyDaysRemaining, mentalConfig.SevereOverdriveEnergyPenaltyDays);
        }

        private void ApplyDailySevereEnergyPenalty()
        {
            if (energyPenaltyDaysRemaining <= 0)
            {
                return;
            }

            ApplyEnergyDelta(mentalConfig.SevereOverdriveDailyEnergyPenalty);
            energyPenaltyDaysRemaining--;
        }

        private void ApplyEnergyDelta(float delta)
        {
            if (Math.Abs(delta) <= 0.0001f)
            {
                return;
            }

            statSystem.ApplyBaseDelta(StatType.Energy, delta);
            var currentEnergy = statSystem.GetStat(StatType.Energy);
            if (currentEnergy < mentalConfig.EnergyFloor)
            {
                statSystem.ApplyBaseDelta(StatType.Energy, mentalConfig.EnergyFloor - currentEnergy);
                currentEnergy = statSystem.GetStat(StatType.Energy);
            }

            UpdateLowestEnergy(currentEnergy);
        }

        private void UpdateLowestEnergy(float value)
        {
            if (value < lowestEnergyThisDay)
            {
                lowestEnergyThisDay = value;
            }
        }

        private bool CanApplyAction(int hours)
        {
            if (hours <= 0 || gameOutcomeSystem.IsResolved)
            {
                return false;
            }

            if (eventManager != null && (eventManager.CurrentEvent != null || eventManager.HasPendingEvents))
            {
                return false;
            }

            return true;
        }

        private float ApplyInflationToExpense(float value)
        {
            if (inflationShockSystem == null)
            {
                return value;
            }

            return inflationShockSystem.ApplyToCost(value);
        }

        private static bool IsMajorCategory(string category)
        {
            return !string.IsNullOrWhiteSpace(category)
                && string.Equals(category.Trim(), "Major", StringComparison.OrdinalIgnoreCase);
        }

        private static float ScaleNegativeCost(float baseDelta, float multiplier)
        {
            if (baseDelta >= 0f)
            {
                return baseDelta;
            }

            return baseDelta * multiplier;
        }

        private void ApplyStudyOutcome(MicroChallengeOutcomeBand outcomeBand)
        {
            switch (outcomeBand)
            {
                case MicroChallengeOutcomeBand.Perfect:
                    statSystem.ApplyBaseDelta(StatType.Academic, StudyPerfectAcademicBonus);
                    break;
                case MicroChallengeOutcomeBand.Poor:
                    statSystem.ApplyBaseDelta(StatType.Academic, StudyPoorAcademicPenalty);
                    statSystem.ApplyBaseDelta(StatType.Mental, StudyPoorMentalPenalty);
                    break;
            }
        }

        private void ApplyWorkOutcome(MicroChallengeOutcomeBand outcomeBand)
        {
            switch (outcomeBand)
            {
                case MicroChallengeOutcomeBand.Perfect:
                    economySystem.ApplyTransaction(WorkPerfectMoneyBonus, "Work quality bonus");
                    break;
                case MicroChallengeOutcomeBand.Poor:
                    economySystem.ApplyTransaction(WorkPoorMoneyPenalty, "Work quality penalty");
                    statSystem.ApplyBaseDelta(StatType.Mental, WorkPoorMentalPenalty);
                    break;
            }
        }

        private void ApplyAdminOutcome(MicroChallengeOutcomeBand outcomeBand)
        {
            switch (outcomeBand)
            {
                case MicroChallengeOutcomeBand.Perfect:
                    economySystem.ApplyTransaction(AdminPerfectMoneyBonus, "Admin refund");
                    break;
                case MicroChallengeOutcomeBand.Poor:
                    economySystem.ApplyTransaction(AdminPoorMoneyPenalty, "Admin late fee");
                    statSystem.ApplyBaseDelta(StatType.Mental, AdminPoorMentalPenalty);
                    break;
            }
        }
    }
}
