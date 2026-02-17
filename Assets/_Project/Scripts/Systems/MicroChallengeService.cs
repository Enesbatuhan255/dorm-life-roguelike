using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class MicroChallengeService : IMicroChallengeService
    {
        private readonly IStatSystem statSystem;
        private readonly ITimeManager timeManager;
        private readonly IEconomySystem economySystem;
        private readonly MicroChallengeConfig config;
        private readonly Func<float> randomRollProvider;
        private int nextSessionId;
        private MicroChallengeSession currentSession;
        private bool hasOpenSession;

        public MicroChallengeService(IStatSystem statSystem, ITimeManager timeManager, IEconomySystem economySystem)
            : this(statSystem, timeManager, economySystem, MicroChallengeConfig.CreateRuntimeDefault(), () => UnityEngine.Random.value)
        {
        }

        public MicroChallengeService(IStatSystem statSystem, ITimeManager timeManager, IEconomySystem economySystem, Func<float> randomRollProvider)
            : this(statSystem, timeManager, economySystem, MicroChallengeConfig.CreateRuntimeDefault(), randomRollProvider)
        {
        }

        public MicroChallengeService(
            IStatSystem statSystem,
            ITimeManager timeManager,
            IEconomySystem economySystem,
            MicroChallengeConfig config,
            Func<float> randomRollProvider)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.economySystem = economySystem ?? throw new ArgumentNullException(nameof(economySystem));
            this.config = config ?? MicroChallengeConfig.CreateRuntimeDefault();
            this.randomRollProvider = randomRollProvider ?? throw new ArgumentNullException(nameof(randomRollProvider));
            nextSessionId = 1;
            hasOpenSession = false;
        }

        public event Action<PlannedActionType, MicroChallengeSession> OnChallengeStarted;
        public event Action<PlannedActionType, MicroChallengeResult> OnChallengeResolved;

        public bool ShouldTrigger(PlannedActionType actionType)
        {
            return actionType == PlannedActionType.Study
                || actionType == PlannedActionType.Admin
                || actionType == PlannedActionType.Work;
        }

        public bool TryBegin(PlannedActionType actionType, out MicroChallengeSession session)
        {
            if (!ShouldTrigger(actionType) || hasOpenSession)
            {
                session = default;
                return false;
            }

            var challengeType = ResolveChallengeType(actionType);
            session = new MicroChallengeSession(
                nextSessionId++,
                actionType,
                challengeType,
                timeManager.Day,
                timeManager.Hour,
                config.DefaultTimeLimitSeconds,
                UnityEngine.Random.Range(int.MinValue, int.MaxValue));

            currentSession = session;
            hasOpenSession = true;
            OnChallengeStarted?.Invoke(actionType, session);
            return true;
        }

        public MicroChallengeResult Complete(MicroChallengeSession session, MicroChallengeInput input)
        {
            if (!hasOpenSession || !session.IsValid || session.SessionId != currentSession.SessionId)
            {
                return new MicroChallengeResult(
                    MicroChallengeGrade.Poor,
                    "Challenge session mismatch.",
                    MicroChallengeOutcomeBand.Poor,
                    0f,
                    true,
                    session.ChallengeType,
                    MicroChallengeAbortReason.InterruptedByEvent);
            }

            var rawScore = ResolveRawScore(input);
            var effectiveScore = ResolveEffectiveScore(rawScore);
            var band = config.ResolveBand(effectiveScore);
            var result = BuildResult(session.ActionType, session.ChallengeType, band, effectiveScore, false, MicroChallengeAbortReason.PlayerCancel);
            FinalizeSession(result);
            return result;
        }

        public MicroChallengeResult Abort(MicroChallengeSession session, MicroChallengeAbortReason reason)
        {
            if (!hasOpenSession || !session.IsValid || session.SessionId != currentSession.SessionId)
            {
                return new MicroChallengeResult(
                    MicroChallengeGrade.Poor,
                    "Challenge already closed.",
                    MicroChallengeOutcomeBand.Poor,
                    0f,
                    true,
                    session.ChallengeType,
                    reason);
            }

            var result = BuildResult(session.ActionType, session.ChallengeType, MicroChallengeOutcomeBand.Poor, 0f, true, reason);
            FinalizeSession(result);
            return result;
        }

        public MicroChallengeResult Resolve(PlannedActionType actionType)
        {
            MicroChallengeSession session;
            if (hasOpenSession && currentSession.ActionType == actionType)
            {
                session = currentSession;
            }
            else if (TryBegin(actionType, out var startedSession))
            {
                session = startedSession;
            }
            else
            {
                return new MicroChallengeResult(MicroChallengeGrade.Good, "No challenge.", MicroChallengeOutcomeBand.Good, 0.5f, false, ResolveChallengeType(actionType), MicroChallengeAbortReason.PlayerCancel);
            }

            // Auto-resolve path used by planner/quick actions until interactive panel flow is wired.
            var baseSkill = RollSkillQuality();
            var input = MicroChallengeInput.FromQuality(baseSkill);
            return Complete(session, input);
        }

        private static float ResolveRawScore(MicroChallengeInput input)
        {
            var hitsFactor = Mathf.Clamp01(input.Hits / 10f);
            var mistakesFactor = Mathf.Clamp01(1f - (input.Mistakes / 5f));
            return Mathf.Clamp01((input.Accuracy * 0.45f) + (input.Speed * 0.35f) + (hitsFactor * 0.15f) + (mistakesFactor * 0.05f));
        }

        private float ResolveEffectiveScore(float rawScore)
        {
            var mental = statSystem.GetStat(StatType.Mental);
            var energy = statSystem.GetStat(StatType.Energy);
            var statModifier = config.ResolveStatModifier(mental, energy);
            return Mathf.Clamp01(rawScore + statModifier);
        }

        private float RollSkillQuality()
        {
            var roll = Mathf.Clamp01(randomRollProvider());
            if (timeManager.IsSecondSemester(timeManager.Day))
            {
                roll = Mathf.Clamp01(roll - 0.1f);
            }

            return roll;
        }

        private MicroChallengeResult BuildResult(
            PlannedActionType actionType,
            MicroChallengeType challengeType,
            MicroChallengeOutcomeBand band,
            float effectiveScore,
            bool wasAborted,
            MicroChallengeAbortReason abortReason)
        {
            var note = BuildNote(actionType, band, effectiveScore, wasAborted, abortReason);
            var grade = ToGrade(band);
            return new MicroChallengeResult(grade, note, band, effectiveScore, wasAborted, challengeType, abortReason);
        }

        private string BuildNote(
            PlannedActionType actionType,
            MicroChallengeOutcomeBand band,
            float effectiveScore,
            bool wasAborted,
            MicroChallengeAbortReason abortReason)
        {
            if (wasAborted)
            {
                return $"Challenge aborted ({abortReason}). Applied as Poor.";
            }

            return $"{actionType} challenge {band} (score {effectiveScore:0.00}, good>={config.GoodScoreMin:0.00}, perfect>={config.PerfectScoreMin:0.00}).";
        }

        private void FinalizeSession(MicroChallengeResult result)
        {
            var actionType = currentSession.ActionType;
            currentSession = default;
            hasOpenSession = false;
            OnChallengeResolved?.Invoke(actionType, result);
        }

        private static MicroChallengeGrade ToGrade(MicroChallengeOutcomeBand band)
        {
            switch (band)
            {
                case MicroChallengeOutcomeBand.Perfect:
                    return MicroChallengeGrade.Perfect;
                case MicroChallengeOutcomeBand.Poor:
                    return MicroChallengeGrade.Poor;
                default:
                    return MicroChallengeGrade.Good;
            }
        }

        private static MicroChallengeType ResolveChallengeType(PlannedActionType actionType)
        {
            switch (actionType)
            {
                case PlannedActionType.Admin:
                    return MicroChallengeType.AdminSort;
                case PlannedActionType.Work:
                    return MicroChallengeType.WorkRhythm;
                default:
                    return MicroChallengeType.StudyFocus;
            }
        }
    }
}
