using System;

namespace DormLifeRoguelike
{
    public sealed class KykSystem : IKykSystem, IDisposable
    {
        private readonly ITimeManager timeManager;
        private readonly IStatSystem statSystem;
        private readonly IEconomySystem economySystem;
        private readonly KykConfig config;
        private bool isDisposed;
        private int monitoringBandDays;
        private int lastPayoutDay;

        public KykSystem(ITimeManager timeManager, IStatSystem statSystem, IEconomySystem economySystem, KykConfig config)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));
            this.config = config ?? throw new ArgumentNullException(nameof(config));

            Status = KykStatus.Normal;
            monitoringBandDays = 0;
            lastPayoutDay = -1;

            this.timeManager.OnDayChanged += HandleDayChanged;
            TryApplyPayoutForDay(this.timeManager.Day);
        }

        public event Action<KykStatus> OnStatusChanged;

        public KykStatus Status { get; private set; }

        public bool IsCut => Status == KykStatus.Cut;

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int day)
        {
            EvaluateStatus();
            TryApplyPayoutForDay(day);
        }

        private void EvaluateStatus()
        {
            if (IsCut)
            {
                return;
            }

            var academic = statSystem.GetStat(StatType.Academic);

            if (academic < config.CriticalAcademicMin)
            {
                SetStatus(KykStatus.Cut);
                return;
            }

            if (academic < config.SafeAcademicMin)
            {
                switch (Status)
                {
                    case KykStatus.Normal:
                        monitoringBandDays = 1;
                        SetStatus(KykStatus.Monitoring);
                        return;
                    case KykStatus.Monitoring:
                        monitoringBandDays++;
                        if (monitoringBandDays >= config.MonitoringDaysToWarning)
                        {
                            SetStatus(KykStatus.Warning);
                        }

                        return;
                    case KykStatus.Warning:
                        SetStatus(KykStatus.Cut);
                        return;
                }
            }

            monitoringBandDays = 0;
            if (Status == KykStatus.Monitoring)
            {
                SetStatus(KykStatus.Normal);
            }
        }

        private void SetStatus(KykStatus next)
        {
            if (Status == next)
            {
                return;
            }

            Status = next;
            if (next == KykStatus.Cut)
            {
                statSystem.ApplyBaseDelta(StatType.Mental, config.CutMentalPenalty);
            }

            OnStatusChanged?.Invoke(next);
        }

        private void TryApplyPayoutForDay(int day)
        {
            if (IsCut || !timeManager.IsKykPayday(day) || lastPayoutDay == day)
            {
                return;
            }

            economySystem.ApplyTransaction(config.MonthlyPaymentAmount, "KYK burs odemesi");
            statSystem.ApplyBaseDelta(StatType.Mental, config.PayoutMentalGain);
            lastPayoutDay = day;
        }
    }
}
