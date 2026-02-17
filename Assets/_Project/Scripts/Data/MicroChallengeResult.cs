namespace DormLifeRoguelike
{
    public sealed class MicroChallengeResult
    {
        public MicroChallengeResult(MicroChallengeGrade grade, string note)
            : this(
                grade,
                note,
                MapBand(grade),
                0f,
                false,
                MicroChallengeType.StudyFocus,
                MicroChallengeAbortReason.PlayerCancel)
        {
        }

        public MicroChallengeResult(
            MicroChallengeGrade grade,
            string note,
            MicroChallengeOutcomeBand outcomeBand,
            float effectiveScore,
            bool wasAborted,
            MicroChallengeType challengeType,
            MicroChallengeAbortReason abortReason)
        {
            Grade = grade;
            Note = note ?? string.Empty;
            OutcomeBand = outcomeBand;
            EffectiveScore = effectiveScore;
            WasAborted = wasAborted;
            ChallengeType = challengeType;
            AbortReason = abortReason;
        }

        public MicroChallengeGrade Grade { get; }

        public string Note { get; }

        public MicroChallengeOutcomeBand OutcomeBand { get; }

        public float EffectiveScore { get; }

        public bool WasAborted { get; }

        public MicroChallengeType ChallengeType { get; }

        public MicroChallengeAbortReason AbortReason { get; }

        private static MicroChallengeOutcomeBand MapBand(MicroChallengeGrade grade)
        {
            switch (grade)
            {
                case MicroChallengeGrade.Perfect:
                    return MicroChallengeOutcomeBand.Perfect;
                case MicroChallengeGrade.Poor:
                    return MicroChallengeOutcomeBand.Poor;
                default:
                    return MicroChallengeOutcomeBand.Good;
            }
        }
    }
}
