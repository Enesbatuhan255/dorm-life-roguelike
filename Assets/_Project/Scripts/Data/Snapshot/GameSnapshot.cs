using System;
using System.Collections.Generic;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class GameSnapshot
    {
        public int schemaVersion = 2;
        public string savedAtUtc = string.Empty;
        public string slotId = string.Empty;
        public TimeSnapshot time = new TimeSnapshot();
        public StatSnapshot stats = new StatSnapshot();
        public FlagSnapshot flags = new FlagSnapshot();
        public EventManagerSnapshot eventManager = new EventManagerSnapshot();
        public EventSchedulerSnapshot eventScheduler = new EventSchedulerSnapshot();
        public GameOutcomeSnapshot gameOutcome = new GameOutcomeSnapshot();
    }

    [Serializable]
    public sealed class TimeSnapshot
    {
        public int day = 1;
        public int hour = 8;
        public int weekIndex = 1;
        public int monthIndex = 1;
    }

    [Serializable]
    public sealed class StatSnapshot
    {
        public float hunger = 100f;
        public float mental = 100f;
        public float energy = 100f;
        public float money = 0f;
        public float academic = 0f;
    }

    [Serializable]
    public sealed class FlagSnapshot
    {
        public List<NumericFlagEntry> numeric = new List<NumericFlagEntry>();
        public List<TextFlagEntry> text = new List<TextFlagEntry>();
    }

    [Serializable]
    public sealed class NumericFlagEntry
    {
        public string key = string.Empty;
        public float value;
    }

    [Serializable]
    public sealed class TextFlagEntry
    {
        public string key = string.Empty;
        public string value = string.Empty;
    }

    [Serializable]
    public sealed class EventManagerSnapshot
    {
        public string currentEventId = string.Empty;
        public List<string> pendingEventIds = new List<string>();
    }

    [Serializable]
    public sealed class EventSchedulerSnapshot
    {
        public int minorQueuedDay = -1;
        public int majorQueuedDay = -1;
        public List<EventCooldownEntrySnapshot> cooldownEntries = new List<EventCooldownEntrySnapshot>();
        public List<ScheduledFollowUpSnapshot> scheduledFollowUps = new List<ScheduledFollowUpSnapshot>();
        public List<FollowUpRepeatSnapshot> pendingFollowUpRepeats = new List<FollowUpRepeatSnapshot>();
    }

    [Serializable]
    public sealed class EventCooldownEntrySnapshot
    {
        public string eventKey = string.Empty;
        public int cooldownUntilHour;
    }

    [Serializable]
    public sealed class ScheduledFollowUpSnapshot
    {
        public string followUpId = string.Empty;
        public int triggerDay = 1;
    }

    [Serializable]
    public sealed class FollowUpRepeatSnapshot
    {
        public string followUpId = string.Empty;
        public int repeatCount;
    }

    [Serializable]
    public sealed class GameOutcomeSnapshot
    {
        public bool isResolved;
        public int consecutiveCriticalAcademicDays;
        public int consecutiveDebtEnforcementDays;
        public GameOutcomeResultSnapshot currentResult = new GameOutcomeResultSnapshot();
    }

    [Serializable]
    public sealed class GameOutcomeResultSnapshot
    {
        public int status;
        public string title = string.Empty;
        public string message = string.Empty;
        public int resolvedOnDay;
        public int score;
        public string scoreBand = "Unrated";
        public int endingId;
        public string epilogTitle = string.Empty;
        public string epilogBody = string.Empty;
        public int debtBand;
        public int employmentState;
    }

    public sealed class SaveSlotSummary
    {
        public SaveSlotSummary(string slotId, bool exists, string savedAtUtc, int day, int hour)
        {
            SlotId = slotId ?? string.Empty;
            Exists = exists;
            SavedAtUtc = savedAtUtc ?? string.Empty;
            Day = day;
            Hour = hour;
        }

        public string SlotId { get; }
        public bool Exists { get; }
        public string SavedAtUtc { get; }
        public int Day { get; }
        public int Hour { get; }
    }
}
