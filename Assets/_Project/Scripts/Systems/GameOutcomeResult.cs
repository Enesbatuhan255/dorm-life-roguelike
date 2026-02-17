namespace DormLifeRoguelike
{
    public enum GameOutcomeStatus
    {
        None,
        Win,
        Lose
    }

    public readonly struct GameOutcomeResult
    {
        public static GameOutcomeResult None => new(
            GameOutcomeStatus.None,
            string.Empty,
            string.Empty,
            0,
            0,
            "Unrated",
            EndingId.None,
            string.Empty,
            string.Empty,
            DebtBand.None,
            EmploymentState.Unknown);

        public GameOutcomeResult(GameOutcomeStatus status, string title, string message, int resolvedOnDay)
            : this(status, title, message, resolvedOnDay, 0, "Unrated")
        {
        }

        public GameOutcomeResult(
            GameOutcomeStatus status,
            string title,
            string message,
            int resolvedOnDay,
            int score,
            string scoreBand)
            : this(
                status,
                title,
                message,
                resolvedOnDay,
                score,
                scoreBand,
                EndingId.None,
                string.Empty,
                string.Empty,
                DebtBand.None,
                EmploymentState.Unknown)
        {
        }

        public GameOutcomeResult(
            GameOutcomeStatus status,
            string title,
            string message,
            int resolvedOnDay,
            int score,
            string scoreBand,
            EndingId endingId,
            string epilogTitle,
            string epilogBody,
            DebtBand debtBand,
            EmploymentState employmentState)
        {
            Status = status;
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            ResolvedOnDay = resolvedOnDay;
            Score = score;
            ScoreBand = string.IsNullOrWhiteSpace(scoreBand) ? "Unrated" : scoreBand;
            EndingId = endingId;
            EpilogTitle = epilogTitle ?? string.Empty;
            EpilogBody = epilogBody ?? string.Empty;
            DebtBand = debtBand;
            EmploymentState = employmentState;
        }

        public GameOutcomeStatus Status { get; }

        public string Title { get; }

        public string Message { get; }

        public int ResolvedOnDay { get; }

        public int Score { get; }

        public string ScoreBand { get; }

        public EndingId EndingId { get; }

        public string EpilogTitle { get; }

        public string EpilogBody { get; }

        public DebtBand DebtBand { get; }

        public EmploymentState EmploymentState { get; }

        public bool IsResolved => Status != GameOutcomeStatus.None;
    }
}
