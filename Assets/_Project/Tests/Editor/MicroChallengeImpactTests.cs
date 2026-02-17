using System;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class MicroChallengeImpactTests
    {
        [Test]
        public void ShouldTrigger_ForStudyAdminAndWork()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.5f);

            Assert.That(service.ShouldTrigger(PlannedActionType.Study), Is.True);
            Assert.That(service.ShouldTrigger(PlannedActionType.Admin), Is.True);
            Assert.That(service.ShouldTrigger(PlannedActionType.Work), Is.True);
            Assert.That(service.ShouldTrigger(PlannedActionType.Sleep), Is.False);

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void HighMentalAndEnergy_ImproveEffectiveScoreBand()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.55f);

            stats.SetBaseValue(StatType.Mental, 85f);
            stats.SetBaseValue(StatType.Energy, 70f);

            var result = service.Resolve(PlannedActionType.Study);

            Assert.That(result.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Good));
            Assert.That(result.EffectiveScore, Is.GreaterThan(0.6f));

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void SecondSemester_MakesRollHarsher()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.82f);

            var firstSemester = service.Resolve(PlannedActionType.Study);
            time.AdvanceTime((37 - 1) * 24);
            var secondSemester = service.Resolve(PlannedActionType.Study);

            Assert.That(firstSemester.EffectiveScore, Is.GreaterThan(secondSemester.EffectiveScore));

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void Abort_ReturnsPoorAbortedResult()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var service = new MicroChallengeService(stats, time, economy, config, () => 0.9f);

            Assert.That(service.TryBegin(PlannedActionType.Work, out var session), Is.True);
            var result = service.Abort(session, MicroChallengeAbortReason.Timeout);

            Assert.That(result.OutcomeBand, Is.EqualTo(MicroChallengeOutcomeBand.Poor));
            Assert.That(result.WasAborted, Is.True);
            Assert.That(result.AbortReason, Is.EqualTo(MicroChallengeAbortReason.Timeout));

            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void DayPlanning_CollectsChallengeNotesDuringExecution()
        {
            var fakeActions = new FakePlayerActionService();
            var time = new TimeManager();
            var stats = new StatSystem();
            var economy = new FakeEconomySystem();
            var config = ScriptableObject.CreateInstance<MicroChallengeConfig>();
            var challengeService = new MicroChallengeService(stats, time, economy, config, () => 0.2f);
            var planning = new DayPlanningService(fakeActions, challengeService);

            planning.TryAddBlock(PlannedActionType.Study);
            planning.TryAddBlock(PlannedActionType.Admin);
            var result = planning.ExecutePlan();

            Assert.That(result.ExecutedBlocks, Is.EqualTo(2));
            Assert.That(result.Notes.Count, Is.GreaterThanOrEqualTo(2));

            UnityEngine.Object.DestroyImmediate(config);
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

        private sealed class FakePlayerActionService : IPlayerActionService
        {
            public int RemainingWorkActionsThisWeek => 99;

            public bool CanWorkThisWeek => true;

            public void ApplyStudy(int hours)
            {
            }

            public void ApplyStudy(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
            }

            public void ApplySleep(int hours)
            {
            }

            public void ApplyWork(int hours)
            {
            }

            public void ApplyWork(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
            }

            public void ApplyWait(int hours)
            {
            }

            public void ApplyAdmin(int hours)
            {
            }

            public void ApplyAdmin(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
            }

            public void ApplySocialize(int hours)
            {
            }

            public bool TryEndDay(out string message)
            {
                message = string.Empty;
                return true;
            }
        }
    }
}
