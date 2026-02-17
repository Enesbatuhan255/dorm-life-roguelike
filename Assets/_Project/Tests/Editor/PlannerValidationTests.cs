using System.Collections.Generic;
using DormLifeRoguelike;
using NUnit.Framework;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class PlannerValidationTests
    {
        [Test]
        public void DayPlan_MaxEightBlocks()
        {
            var fakeAction = new FakePlayerActionService();
            var planning = new DayPlanningService(fakeAction);

            for (var i = 0; i < DayPlan.MaxBlocksPerDay; i++)
            {
                Assert.That(planning.TryAddBlock(PlannedActionType.Study), Is.True);
            }

            Assert.That(planning.TryAddBlock(PlannedActionType.Study), Is.False);
            Assert.That(planning.CurrentPlan.BlockCount, Is.EqualTo(8));
        }

        [Test]
        public void ExecutePlan_InvokesMappedActions()
        {
            var fakeAction = new FakePlayerActionService();
            var planning = new DayPlanningService(fakeAction);

            planning.TryAddBlock(PlannedActionType.Study);
            planning.TryAddBlock(PlannedActionType.Work);
            planning.TryAddBlock(PlannedActionType.Sleep);
            planning.TryAddBlock(PlannedActionType.Socialize);
            planning.TryAddBlock(PlannedActionType.Wait);
            planning.TryAddBlock(PlannedActionType.Admin);

            var result = planning.ExecutePlan();

            Assert.That(result.ExecutedBlocks, Is.EqualTo(6));
            Assert.That(result.RejectedBlocks, Is.EqualTo(0));
            Assert.That(fakeAction.Log, Is.EqualTo(new[]
            {
                "Study:2",
                "Work:2",
                "Sleep:2",
                "Socialize:2",
                "Wait:2",
                "Admin:2"
            }));
            Assert.That(planning.CurrentPlan.BlockCount, Is.EqualTo(0));
        }

        [Test]
        public void SimulatePlan_WarnsForWorkOverLimit()
        {
            var fakeAction = new FakePlayerActionService { RemainingWorkActionsThisWeek = 1 };
            var planning = new DayPlanningService(fakeAction);

            planning.TryAddBlock(PlannedActionType.Work);
            planning.TryAddBlock(PlannedActionType.Work);

            var sim = planning.SimulatePlan();

            Assert.That(sim.RejectedBlocks, Is.EqualTo(1));
            Assert.That(sim.Notes.Count, Is.GreaterThan(0));
        }

        private sealed class FakePlayerActionService : IPlayerActionService
        {
            private readonly List<string> log = new List<string>();

            public IReadOnlyList<string> Log => log;

            public int RemainingWorkActionsThisWeek { get; set; } = 99;

            public bool CanWorkThisWeek => RemainingWorkActionsThisWeek > 0;

            public void ApplyStudy(int hours)
            {
                log.Add($"Study:{hours}");
            }

            public void ApplyStudy(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
                log.Add($"Study:{hours}");
            }

            public void ApplySleep(int hours)
            {
                log.Add($"Sleep:{hours}");
            }

            public void ApplyWork(int hours)
            {
                if (RemainingWorkActionsThisWeek > 0)
                {
                    RemainingWorkActionsThisWeek--;
                }

                log.Add($"Work:{hours}");
            }

            public void ApplyWork(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
                ApplyWork(hours);
            }

            public void ApplyWait(int hours)
            {
                log.Add($"Wait:{hours}");
            }

            public void ApplyAdmin(int hours)
            {
                log.Add($"Admin:{hours}");
            }

            public void ApplyAdmin(int hours, MicroChallengeOutcomeBand outcomeBand)
            {
                ApplyAdmin(hours);
            }

            public void ApplySocialize(int hours)
            {
                log.Add($"Socialize:{hours}");
            }

            public bool TryEndDay(out string message)
            {
                message = "";
                return true;
            }
        }
    }
}
