namespace DormLifeRoguelike
{
    public interface IPlayerActionService : IService
    {
        int RemainingWorkActionsThisWeek { get; }

        bool CanWorkThisWeek { get; }

        void ApplyStudy(int hours);
        void ApplyStudy(int hours, MicroChallengeOutcomeBand outcomeBand);

        void ApplySleep(int hours);

        void ApplyWork(int hours);
        void ApplyWork(int hours, MicroChallengeOutcomeBand outcomeBand);

        void ApplyWait(int hours);

        void ApplyAdmin(int hours);
        void ApplyAdmin(int hours, MicroChallengeOutcomeBand outcomeBand);

        void ApplySocialize(int hours);

        bool TryEndDay(out string message);
    }
}
