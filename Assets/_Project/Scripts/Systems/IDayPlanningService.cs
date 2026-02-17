namespace DormLifeRoguelike
{
    public interface IDayPlanningService : IService
    {
        DayPlan CurrentPlan { get; }

        bool TryAddBlock(PlannedActionType actionType);

        void ClearPlan();

        PlanSimulationResult SimulatePlan();

        PlanSimulationResult ExecutePlan();
    }
}
