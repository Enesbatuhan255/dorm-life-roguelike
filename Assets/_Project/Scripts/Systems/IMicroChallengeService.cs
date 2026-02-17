using System;

namespace DormLifeRoguelike
{
    public interface IMicroChallengeService : IService
    {
        event Action<PlannedActionType, MicroChallengeSession> OnChallengeStarted;
        event Action<PlannedActionType, MicroChallengeResult> OnChallengeResolved;

        bool ShouldTrigger(PlannedActionType actionType);

        bool TryBegin(PlannedActionType actionType, out MicroChallengeSession session);

        MicroChallengeResult Complete(MicroChallengeSession session, MicroChallengeInput input);

        MicroChallengeResult Abort(MicroChallengeSession session, MicroChallengeAbortReason reason);

        MicroChallengeResult Resolve(PlannedActionType actionType);
    }
}
