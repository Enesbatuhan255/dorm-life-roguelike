using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public sealed class DayPlanningService : IDayPlanningService
    {
        private readonly IPlayerActionService playerActionService;
        private readonly IMicroChallengeService microChallengeService;
        private readonly DayPlan plan = new DayPlan();

        public DayPlanningService(IPlayerActionService playerActionService)
            : this(playerActionService, null)
        {
        }

        public DayPlanningService(IPlayerActionService playerActionService, IMicroChallengeService microChallengeService)
        {
            this.playerActionService = playerActionService;
            this.microChallengeService = microChallengeService;
        }

        public DayPlan CurrentPlan => plan;

        public bool TryAddBlock(PlannedActionType actionType)
        {
            return plan.TryAddBlock(actionType);
        }

        public void ClearPlan()
        {
            plan.Clear();
        }

        public PlanSimulationResult SimulatePlan()
        {
            var notes = new List<string>();
            var rejected = 0;

            var projectedWorkSlots = 0;
            for (var i = 0; i < plan.Blocks.Count; i++)
            {
                var action = plan.Blocks[i];
                if (action != PlannedActionType.Work)
                {
                    continue;
                }

                projectedWorkSlots++;
                if (projectedWorkSlots > playerActionService.RemainingWorkActionsThisWeek)
                {
                    rejected++;
                    notes.Add("Work limit will block some planned work blocks.");
                    break;
                }
            }

            if (plan.BlockCount == 0)
            {
                notes.Add("Plan is empty.");
            }

            return new PlanSimulationResult(plan.BlockCount - rejected, rejected, notes);
        }

        public PlanSimulationResult ExecutePlan()
        {
            var notes = new List<string>();
            var executed = 0;
            var rejected = 0;

            for (var i = 0; i < plan.Blocks.Count; i++)
            {
                var action = plan.Blocks[i];
                var outcomeBand = ResolveOutcomeBand(action, i, notes);
                var didApply = ApplyAction(action, outcomeBand);
                if (didApply)
                {
                    executed++;
                }
                else
                {
                    rejected++;
                    notes.Add($"Blocked at block {i + 1}: {action}.");
                }
            }

            plan.Clear();
            return new PlanSimulationResult(executed, rejected, notes);
        }

        private MicroChallengeOutcomeBand ResolveOutcomeBand(PlannedActionType actionType, int blockIndex, List<string> notes)
        {
            if (microChallengeService == null || !microChallengeService.ShouldTrigger(actionType))
            {
                return MicroChallengeOutcomeBand.Good;
            }

            if (!microChallengeService.TryBegin(actionType, out var session))
            {
                notes.Add($"Block {blockIndex + 1} challenge: skipped.");
                return MicroChallengeOutcomeBand.Good;
            }

            var challengeResult = microChallengeService.Resolve(actionType);
            if (!string.IsNullOrWhiteSpace(challengeResult.Note))
            {
                notes.Add($"Block {blockIndex + 1} challenge: {challengeResult.Note}");
            }

            return challengeResult.OutcomeBand;
        }

        private bool ApplyAction(PlannedActionType actionType, MicroChallengeOutcomeBand outcomeBand)
        {
            switch (actionType)
            {
                case PlannedActionType.Study:
                    playerActionService.ApplyStudy(DayPlan.HoursPerBlock, outcomeBand);
                    return true;
                case PlannedActionType.Work:
                    if (!playerActionService.CanWorkThisWeek)
                    {
                        return false;
                    }

                    playerActionService.ApplyWork(DayPlan.HoursPerBlock, outcomeBand);
                    return true;
                case PlannedActionType.Sleep:
                    playerActionService.ApplySleep(DayPlan.HoursPerBlock);
                    return true;
                case PlannedActionType.Socialize:
                    playerActionService.ApplySocialize(DayPlan.HoursPerBlock);
                    return true;
                case PlannedActionType.Wait:
                    playerActionService.ApplyWait(DayPlan.HoursPerBlock);
                    return true;
                case PlannedActionType.Admin:
                    playerActionService.ApplyAdmin(DayPlan.HoursPerBlock, outcomeBand);
                    return true;
                default:
                    return false;
            }
        }
    }
}
