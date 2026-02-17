using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class MicroChallengeScoringTests
    {
        [Test]
        public void Complete_MapsEffectiveScoreToBands()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Mental, 50f);
            stats.SetBaseValue(StatType.Energy, 40f);
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.5f);

            Assert.That(service.TryBegin(PlannedActionType.Study, out var session), Is.True);
            var poor = service.Complete(session, MicroChallengeInput.FromQuality(0.2f));

            Assert.That(service.TryBegin(PlannedActionType.Study, out session), Is.True);
            var good = service.Complete(session, MicroChallengeInput.FromQuality(0.55f));

            Assert.That(service.TryBegin(PlannedActionType.Study, out session), Is.True);
            var perfect = service.Complete(session, MicroChallengeInput.FromQuality(0.9f));

            Assert.That(poor.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Poor));
            Assert.That(good.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Good));
            Assert.That(perfect.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Perfect));

            Object.DestroyImmediate(config);
        }

        [Test]
        public void NegativeEnergyAndLowMental_DowngradeBand()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Mental, 20f);
            stats.SetBaseValue(StatType.Energy, -5f);
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.6f);

            Assert.That(service.TryBegin(PlannedActionType.Work, out var session), Is.True);
            var result = service.Complete(session, MicroChallengeInput.FromQuality(0.6f));

            Assert.That(result.EffectiveScore, Is.LessThan(0.45f));
            Assert.That(result.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Poor));

            Object.DestroyImmediate(config);
        }

        private sealed class FakeEconomySystem : IEconomySystem
        {
            public event System.Action<float, string> OnTransactionApplied;

            public bool CanAfford(float cost)
            {
                return true;
            }

            public void ApplyTransaction(float amount, string reason)
            {
                OnTransactionApplied?.Invoke(amount, reason);
            }

            public void ApplyDailyCosts()
            {
            }
        }
    }
}
