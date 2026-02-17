using System;
using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public sealed class FlagRuleService : IFlagRuleService, IDisposable
    {
        private readonly IFlagStateService flagStateService;
        private readonly IStatSystem statSystem;
        private readonly ITimeManager timeManager;
        private readonly IEventManager eventManager;

        private bool isDisposed;

        public FlagRuleService(
            IFlagStateService flagStateService,
            IStatSystem statSystem,
            ITimeManager timeManager,
            IEventManager eventManager)
        {
            this.flagStateService = flagStateService ?? throw new ArgumentNullException(nameof(flagStateService));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.eventManager = eventManager;

            this.timeManager.OnDayChanged += HandleDayChanged;
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

        private void HandleDayChanged(int _)
        {
            ApplyDebtPressure();
            ApplyWorkStrain();
            ApplyBurnout();
            ApplyKykRiskDays();
            ApplyIllegalFinePending();
        }

        private void ApplyDebtPressure()
        {
            if (!flagStateService.TryGetNumeric("debt_pressure", out var debtPressure))
            {
                return;
            }

            if (debtPressure > 0f)
            {
                var moneyPenalty = -Math.Min(120f, debtPressure * 15f);
                var mentalPenalty = -Math.Min(6f, debtPressure * 0.5f);
                statSystem.ApplyBaseDelta(StatType.Money, moneyPenalty);
                statSystem.ApplyBaseDelta(StatType.Mental, mentalPenalty);
                eventManager?.PublishSystemMessage("Borc baskisi gunluk giderleri artirdi.");
                return;
            }

            if (debtPressure < 0f)
            {
                var recovery = Math.Min(3f, -debtPressure * 0.25f);
                statSystem.ApplyBaseDelta(StatType.Mental, recovery);
            }
        }

        private void ApplyWorkStrain()
        {
            if (!flagStateService.TryGetNumeric("work_strain", out var workStrain) || workStrain <= 0f)
            {
                return;
            }

            var energyPenalty = -Math.Min(5f, workStrain * 0.7f);
            var mentalPenalty = -Math.Min(4f, workStrain * 0.5f);
            statSystem.ApplyBaseDelta(StatType.Energy, energyPenalty);
            statSystem.ApplyBaseDelta(StatType.Mental, mentalPenalty);
            DecreaseNumericFlag("work_strain", 1f);
        }

        private void ApplyBurnout()
        {
            if (!flagStateService.TryGetNumeric("burnout", out var burnout) || burnout <= 0f)
            {
                return;
            }

            var mentalPenalty = -Math.Min(8f, burnout);
            var energyPenalty = -Math.Min(6f, burnout * 0.8f);
            statSystem.ApplyBaseDelta(StatType.Mental, mentalPenalty);
            statSystem.ApplyBaseDelta(StatType.Energy, energyPenalty);
            DecreaseNumericFlag("burnout", 1f);
        }

        private void ApplyKykRiskDays()
        {
            if (!flagStateService.TryGetNumeric("kyk_risk_days", out var riskDays) || riskDays <= 0f)
            {
                return;
            }

            var academicPenalty = -Math.Min(0.12f, riskDays * 0.03f);
            var mentalPenalty = -Math.Min(2f, riskDays * 0.3f);
            statSystem.ApplyBaseDelta(StatType.Academic, academicPenalty);
            statSystem.ApplyBaseDelta(StatType.Mental, mentalPenalty);
            DecreaseNumericFlag("kyk_risk_days", 1f);
        }

        private void ApplyIllegalFinePending()
        {
            if (!flagStateService.TryGetNumeric("illegal_fine_pending", out var pending) || pending <= 0f)
            {
                return;
            }

            statSystem.ApplyBaseDelta(StatType.Money, -200f);
            statSystem.ApplyBaseDelta(StatType.Mental, -2f);
            flagStateService.ApplyChanges(new[]
            {
                BuildNumericChange("debt_pressure", 1f)
            });
            DecreaseNumericFlag("illegal_fine_pending", 1f);
            eventManager?.PublishSystemMessage("Bekleyen kumar cezasi bugun ekstra yuk getirdi.");
        }

        private void DecreaseNumericFlag(string key, float amount)
        {
            flagStateService.ApplyChanges(new[]
            {
                BuildNumericChange(key, -Math.Abs(amount))
            });
        }

        private static EventFlagChange BuildNumericChange(string key, float delta)
        {
            return new EventFlagChangeBuilder()
                .WithKey(key)
                .WithMode(EventFlagChangeMode.AddNumeric)
                .WithNumeric(delta)
                .Build();
        }

        private sealed class EventFlagChangeBuilder
        {
            private readonly EventFlagChange value = new EventFlagChange();

            public EventFlagChangeBuilder WithKey(string key)
            {
                SetField("key", key ?? string.Empty);
                return this;
            }

            public EventFlagChangeBuilder WithMode(EventFlagChangeMode mode)
            {
                SetField("mode", mode);
                return this;
            }

            public EventFlagChangeBuilder WithNumeric(float numericValue)
            {
                SetField("numericValue", numericValue);
                return this;
            }

            public EventFlagChange Build()
            {
                return value;
            }

            private void SetField<T>(string fieldName, T fieldValue)
            {
                var field = typeof(EventFlagChange).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                field?.SetValue(value, fieldValue);
            }
        }
    }
}
