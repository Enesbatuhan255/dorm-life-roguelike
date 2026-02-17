using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class SleepDebtSystem : ISleepDebtSystem, IDisposable
    {
        private const float MaxSleepDebt = 100f;

        private readonly ITimeManager timeManager;
        private readonly IStatSystem statSystem;
        private readonly SleepDebtConfig config;

        private bool isDisposed;
        private bool isSleeping;
        private int pendingSleepHours;
        private float sleepDebt;

        public SleepDebtSystem(ITimeManager timeManager, IStatSystem statSystem, SleepDebtConfig config)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            sleepDebt = 0f;
            pendingSleepHours = 0;

            this.timeManager.OnTimeAdvanced += HandleTimeAdvanced;
        }

        public event Action<float> OnSleepDebtChanged;

        public float SleepDebt => sleepDebt;

        public float GetEnergyCostMultiplier()
        {
            return 1f + (sleepDebt / MaxSleepDebt) * 0.6f;
        }

        public float GetMentalCostMultiplier()
        {
            return 1f + (sleepDebt / MaxSleepDebt) * 0.4f;
        }

        public void ApplySleepAction(int sleepHours)
        {
            if (sleepHours <= 0)
            {
                return;
            }

            SetSleepDebt(sleepDebt - sleepHours * config.SleepDebtReductionPerHour, "sleep action");

            statSystem.ApplyBaseDelta(StatType.Energy, sleepHours * config.SleepEnergyRecoveryPerHour);
            statSystem.ApplyBaseDelta(StatType.Mental, sleepHours * config.SleepMentalRecoveryPerHour);
        }

        public void BeginSleep(int sleepHours)
        {
            if (sleepHours <= 0)
            {
                return;
            }

            isSleeping = true;
            pendingSleepHours += sleepHours;
        }

        public void EndSleep()
        {
            isSleeping = false;
            pendingSleepHours = 0;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnTimeAdvanced -= HandleTimeAdvanced;
        }

        private void HandleTimeAdvanced(TimeChangedEventArgs args)
        {
            if (args == null || args.AdvancedHours <= 0)
            {
                return;
            }

            var simulatedHour = Mathf.Clamp(args.OldHour, 0, 23);
            var nightAwakeHours = 0;

            for (var i = 0; i < args.AdvancedHours; i++)
            {
                var hasSleepSuppression = isSleeping && pendingSleepHours > 0;
                if (hasSleepSuppression)
                {
                    pendingSleepHours--;
                }
                else if (IsNightHour(simulatedHour))
                {
                    nightAwakeHours++;
                }

                simulatedHour = (simulatedHour + 1) % 24;
            }

            if (nightAwakeHours <= 0)
            {
                return;
            }

            var delta = nightAwakeHours * config.NightHourDebtIncrease;
            SetSleepDebt(sleepDebt + delta, $"night awake hours: {nightAwakeHours}");
        }

        private void SetSleepDebt(float value, string reason)
        {
            var clamped = Mathf.Clamp(value, 0f, MaxSleepDebt);
            if (Mathf.Approximately(clamped, sleepDebt))
            {
                return;
            }

            sleepDebt = clamped;
            Debug.Log($"[SleepDebtSystem] SleepDebt -> {sleepDebt:0.##} ({reason})");
            OnSleepDebtChanged?.Invoke(sleepDebt);
        }

        private static bool IsNightHour(int hour)
        {
            return hour >= 23 || hour < 6;
        }
    }
}
