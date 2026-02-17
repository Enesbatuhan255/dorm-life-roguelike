namespace DormLifeRoguelike
{
    public readonly struct MicroChallengeSession
    {
        public MicroChallengeSession(
            int sessionId,
            PlannedActionType actionType,
            MicroChallengeType challengeType,
            int startedDay,
            int startedHour,
            float timeLimitSeconds,
            int seed)
        {
            SessionId = sessionId;
            ActionType = actionType;
            ChallengeType = challengeType;
            StartedDay = startedDay;
            StartedHour = startedHour;
            TimeLimitSeconds = timeLimitSeconds;
            Seed = seed;
        }

        public int SessionId { get; }

        public PlannedActionType ActionType { get; }

        public MicroChallengeType ChallengeType { get; }

        public int StartedDay { get; }

        public int StartedHour { get; }

        public float TimeLimitSeconds { get; }

        public int Seed { get; }

        public bool IsValid => SessionId > 0;
    }
}
