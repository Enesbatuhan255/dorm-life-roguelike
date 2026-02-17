using System.Collections;

namespace DormLifeRoguelike
{
    public sealed partial class ActionPanelPresenter
    {
        private MicroChallengeOutcomeBand ResolveOutcomeBandForAction(PlannedActionType actionType)
        {
            if (microChallengeService == null || !microChallengeService.ShouldTrigger(actionType))
            {
                return MicroChallengeOutcomeBand.Good;
            }

            return microChallengeService.Resolve(actionType).OutcomeBand;
        }

        private bool TryRunInteractiveChallenge(PlannedActionType actionType, int hours)
        {
            if (microChallengeService == null || microChallengeRunner == null || microChallengeRunner.IsRunning)
            {
                return false;
            }

            if (!microChallengeService.TryBegin(actionType, out var session))
            {
                return false;
            }

            if (!microChallengeRunner.TryRun(session, (input, wasTimedOut) =>
            {
                StartCoroutine(CompleteInteractiveChallenge(actionType, hours, session, input, wasTimedOut));
            }))
            {
                microChallengeService.Abort(session, MicroChallengeAbortReason.PlayerCancel);
                return false;
            }

            isInteractiveChallengePending = true;
            RefreshActionAvailability();
            return true;
        }

        private IEnumerator CompleteInteractiveChallenge(PlannedActionType actionType, int hours, MicroChallengeSession session, MicroChallengeInput input, bool wasTimedOut)
        {
            yield return null;
            if (!TryBindServices() || playerActionService == null || microChallengeService == null)
            {
                isInteractiveChallengePending = false;
                RefreshActionAvailability();
                yield break;
            }

            var result = wasTimedOut
                ? microChallengeService.Abort(session, MicroChallengeAbortReason.Timeout)
                : microChallengeService.Complete(session, input);

            if (actionType == PlannedActionType.Admin)
            {
                playerActionService.ApplyAdmin(hours, result.OutcomeBand);
            }
            else if (actionType == PlannedActionType.Work)
            {
                playerActionService.ApplyWork(hours, result.OutcomeBand);
            }
            else
            {
                playerActionService.ApplyStudy(hours, result.OutcomeBand);
            }

            isInteractiveChallengePending = false;
            RefreshActionAvailability();
            var shortNote = string.IsNullOrWhiteSpace(result.Note) ? string.Empty : $" | {result.Note}";
            RefreshFeedback($"{actionType} {result.OutcomeBand}{shortNote}");
        }
    }
}
