using System;

namespace DormLifeRoguelike
{
    public interface IMicroChallengeInteractiveRunner
    {
        bool IsRunning { get; }

        bool TryRun(MicroChallengeSession session, Action<MicroChallengeInput, bool> onCompleted);
    }
}
