using System;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class KykSystemTests
    {
        private KykConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<KykConfig>();
            config.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void CutStatus_StopsMonthlyPayment()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var fakeEconomy = new FakeEconomySystem();
            using var kyk = new KykSystem(time, stats, fakeEconomy, config);

            stats.SetBaseValue(StatType.Academic, 1.7f);
            time.AdvanceTime(24);

            Assert.That(kyk.Status, Is.EqualTo(KykStatus.Cut));

            var before = fakeEconomy.TotalApplied;
            time.AdvanceTime(24 * 80);
            Assert.That(fakeEconomy.TotalApplied, Is.EqualTo(before).Within(0.001f));
        }

        [Test]
        public void Payday_AppliesMoneyAndMental_WhenNotCut()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var fakeEconomy = new FakeEconomySystem();
            stats.SetBaseValue(StatType.Mental, 50f);

            using var kyk = new KykSystem(time, stats, fakeEconomy, config);

            Assert.That(fakeEconomy.TotalApplied, Is.EqualTo(1500f).Within(0.001f));
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(53f).Within(0.001f));
        }

        [Test]
        public void CutStatus_AppliesMentalPenaltyOnce()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var fakeEconomy = new FakeEconomySystem();
            stats.SetBaseValue(StatType.Mental, 50f);
            using var kyk = new KykSystem(time, stats, fakeEconomy, config);

            stats.SetBaseValue(StatType.Mental, 50f);
            stats.SetBaseValue(StatType.Academic, 1.7f);

            time.AdvanceTime(24);
            var afterCutMental = stats.GetStat(StatType.Mental);

            Assert.That(afterCutMental, Is.EqualTo(30f).Within(0.001f));

            time.AdvanceTime(24);
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(afterCutMental).Within(0.001f));
        }

        private sealed class FakeEconomySystem : IEconomySystem
        {
            public event Action<float, string> OnTransactionApplied;

            public float TotalApplied { get; private set; }

            public bool CanAfford(float cost)
            {
                return true;
            }

            public void ApplyTransaction(float amount, string reason)
            {
                TotalApplied += amount;
                OnTransactionApplied?.Invoke(amount, reason);
            }

            public void ApplyDailyCosts()
            {
            }
        }
    }
}
