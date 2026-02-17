namespace DormLifeRoguelike
{
    public interface IWorkLimitSystem : IService
    {
        int MaxWorkActionsPerWeek { get; }

        int WorkActionsUsedThisWeek { get; }

        int RemainingWorkActionsThisWeek { get; }

        bool CanWork();

        bool TryConsumeWorkAction();
    }
}
