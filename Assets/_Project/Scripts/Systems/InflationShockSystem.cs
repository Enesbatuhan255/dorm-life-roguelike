using System;

namespace DormLifeRoguelike
{
    public sealed class InflationShockSystem : IInflationShockSystem, IDisposable
    {
        private readonly ITimeManager timeManager;
        private readonly InflationShockConfig config;
        private bool isDisposed;

        public InflationShockSystem(ITimeManager timeManager, InflationShockConfig config)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            this.timeManager.OnDayChanged += HandleDayChanged;
            IsTriggered = this.timeManager.Day >= this.config.TriggerDay;
        }

        public bool IsTriggered { get; private set; }

        public float CurrentMultiplier => IsTriggered ? config.Multiplier : 1f;

        public float ApplyToCost(float amount)
        {
            if (!IsTriggered || amount >= 0f)
            {
                return amount;
            }

            return amount * config.Multiplier;
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

        private void HandleDayChanged(int day)
        {
            if (IsTriggered)
            {
                return;
            }

            if (day >= config.TriggerDay)
            {
                IsTriggered = true;
            }
        }
    }
}
