namespace DormLifeRoguelike
{
    public sealed class SnapshotMigrator : ISnapshotMigrator
    {
        public const int CurrentSchemaVersion = 2;

        public GameSnapshot Migrate(GameSnapshot source)
        {
            if (source == null)
            {
                return null;
            }

            if (source.schemaVersion <= 0)
            {
                source.schemaVersion = 1;
            }

            if (source.time == null)
            {
                source.time = new TimeSnapshot();
            }

            if (source.stats == null)
            {
                source.stats = new StatSnapshot();
            }

            if (source.flags == null)
            {
                source.flags = new FlagSnapshot();
            }

            if (source.flags.numeric == null)
            {
                source.flags.numeric = new System.Collections.Generic.List<NumericFlagEntry>();
            }

            if (source.flags.text == null)
            {
                source.flags.text = new System.Collections.Generic.List<TextFlagEntry>();
            }

            if (source.eventManager == null)
            {
                source.eventManager = new EventManagerSnapshot();
            }

            if (source.eventManager.pendingEventIds == null)
            {
                source.eventManager.pendingEventIds = new System.Collections.Generic.List<string>();
            }

            if (source.eventScheduler == null)
            {
                source.eventScheduler = new EventSchedulerSnapshot();
            }

            if (source.eventScheduler.cooldownEntries == null)
            {
                source.eventScheduler.cooldownEntries = new System.Collections.Generic.List<EventCooldownEntrySnapshot>();
            }

            if (source.eventScheduler.scheduledFollowUps == null)
            {
                source.eventScheduler.scheduledFollowUps = new System.Collections.Generic.List<ScheduledFollowUpSnapshot>();
            }

            if (source.eventScheduler.pendingFollowUpRepeats == null)
            {
                source.eventScheduler.pendingFollowUpRepeats = new System.Collections.Generic.List<FollowUpRepeatSnapshot>();
            }

            if (source.gameOutcome == null)
            {
                source.gameOutcome = new GameOutcomeSnapshot();
            }

            if (source.gameOutcome.currentResult == null)
            {
                source.gameOutcome.currentResult = new GameOutcomeResultSnapshot();
            }

            if (source.schemaVersion > CurrentSchemaVersion)
            {
                return null;
            }

            source.schemaVersion = CurrentSchemaVersion;

            return source;
        }
    }
}
