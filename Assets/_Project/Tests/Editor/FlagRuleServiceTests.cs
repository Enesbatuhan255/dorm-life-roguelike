using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class FlagRuleServiceTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void DayChanged_AppliesFlagDrivenStatEffects_AndDecaysCounters()
        {
            var statSystem = new StatSystem();
            statSystem.SetBaseValue(StatType.Academic, 2f);
            var timeManager = new TimeManager();
            var flagState = new FlagStateService();
            var eventManager = new EventManager(statSystem, timeManager, flagState);

            flagState.ApplyChanges(new[]
            {
                CreateNumericFlag("debt_pressure", 4f),
                CreateNumericFlag("work_strain", 2f),
                CreateNumericFlag("burnout", 1f),
                CreateNumericFlag("kyk_risk_days", 2f),
                CreateNumericFlag("illegal_fine_pending", 1f)
            });

            using var rules = new FlagRuleService(flagState, statSystem, timeManager, eventManager);
            timeManager.AdvanceTime(24);

            Assert.That(statSystem.GetStat(StatType.Money), Is.EqualTo(-260f).Within(0.01f));
            Assert.That(statSystem.GetStat(StatType.Mental), Is.EqualTo(93.4f).Within(0.1f));
            Assert.That(statSystem.GetStat(StatType.Energy), Is.EqualTo(97.8f).Within(0.1f));
            Assert.That(statSystem.GetStat(StatType.Academic), Is.EqualTo(1.94f).Within(0.01f));

            Assert.That(flagState.TryGetNumeric("debt_pressure", out var debtPressure), Is.True);
            Assert.That(debtPressure, Is.EqualTo(5f).Within(0.01f));
            Assert.That(flagState.TryGetNumeric("work_strain", out var workStrain), Is.True);
            Assert.That(workStrain, Is.EqualTo(1f).Within(0.01f));
            Assert.That(flagState.TryGetNumeric("burnout", out var burnout), Is.True);
            Assert.That(burnout, Is.EqualTo(0f).Within(0.01f));
            Assert.That(flagState.TryGetNumeric("kyk_risk_days", out var kykRiskDays), Is.True);
            Assert.That(kykRiskDays, Is.EqualTo(1f).Within(0.01f));
            Assert.That(flagState.TryGetNumeric("illegal_fine_pending", out var pendingFine), Is.True);
            Assert.That(pendingFine, Is.EqualTo(0f).Within(0.01f));
        }

        private static EventFlagChange CreateNumericFlag(string key, float value)
        {
            var flag = new EventFlagChange();
            SetField(flag, "key", key);
            SetField(flag, "mode", EventFlagChangeMode.AddNumeric);
            SetField(flag, "numericValue", value);
            SetField(flag, "textValue", string.Empty);
            return flag;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field?.SetValue(target, value);
        }
    }
}
