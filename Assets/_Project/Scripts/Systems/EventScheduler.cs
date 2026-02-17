using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed partial class EventScheduler : IEventScheduler, IDisposable
    {
        private sealed class ScheduledFollowUp
        {
            public string FollowUpId;
            public int TriggerDay;
        }

        private const string MinorCategory = "minor";
        private const string MajorCategory = "major";

        private readonly ITimeManager timeManager;
        private readonly IEventManager eventManager;
        private readonly IStatSystem statSystem;
        private readonly IFlagStateService flagStateService;
        private readonly List<EventData> minorEventPool = new List<EventData>();
        private readonly List<EventData> majorEventPool = new List<EventData>();
        private readonly List<EventData> eligibleEvents = new List<EventData>();
        private readonly Dictionary<string, int> eventCooldownUntilHour = new Dictionary<string, int>();
        private readonly HashSet<int> missingEventIdWarnings = new HashSet<int>();
        private readonly Dictionary<string, EventData> eventsById = new Dictionary<string, EventData>();
        private readonly HashSet<string> missingFollowUpWarnings = new HashSet<string>();
        private readonly List<ScheduledFollowUp> scheduledFollowUps = new List<ScheduledFollowUp>();
        private readonly Dictionary<string, int> pendingFollowUpRepeatCounts = new Dictionary<string, int>();
        private readonly EventCooldownConfig cooldownConfig;

        private bool isDisposed;
        private int minorQueuedDay = -1;
        private int majorQueuedDay = -1;

        public EventScheduler(
            ITimeManager timeManager,
            IEventManager eventManager,
            IEnumerable<EventData> events,
            int checkIntervalHours,
            int cooldownHours)
            : this(
                timeManager,
                eventManager,
                null,
                events,
                checkIntervalHours,
                EventCooldownConfig.CreateRuntimeDefault(cooldownHours),
                null)
        {
        }

        public EventScheduler(
            ITimeManager timeManager,
            IEventManager eventManager,
            IEnumerable<EventData> events,
            int checkIntervalHours,
            EventCooldownConfig cooldownConfig)
            : this(
                timeManager,
                eventManager,
                null,
                events,
                checkIntervalHours,
                cooldownConfig,
                null)
        {
        }

        public EventScheduler(
            ITimeManager timeManager,
            IEventManager eventManager,
            IStatSystem statSystem,
            IEnumerable<EventData> events,
            int checkIntervalHours,
            EventCooldownConfig cooldownConfig,
            IFlagStateService flagStateService = null)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.eventManager = eventManager ?? throw new ArgumentNullException(nameof(eventManager));
            this.statSystem = statSystem;
            this.cooldownConfig = cooldownConfig ?? throw new ArgumentNullException(nameof(cooldownConfig));
            this.flagStateService = flagStateService;

            PopulatePools(events);

            timeManager.OnDayChanged += HandleDayChanged;
            eventManager.OnChoiceApplied += HandleChoiceApplied;
            eventManager.OnEventCompleted += HandleEventCompleted;
            TryQueueMinorForCurrentDay();
        }

        public void ForceEvaluate()
        {
            TryQueueMinorForCurrentDay();
        }

        public EventData PickMinorEventForDay(int day, int hour)
        {
            var nowAbsHour = GetAbsoluteHour(day, hour);
            return PickEvent(minorEventPool, nowAbsHour, day);
        }

        public EventData PickMajorEventForDay(int day, int hour)
        {
            var nowAbsHour = GetAbsoluteHour(day, hour);

            // Guaranteed calendar majors are selected before generic weighted major events.
            if (timeManager.IsInExamWindow(day))
            {
                var examEvent = PickPriorityMajor(nowAbsHour, day, IsExamMajorEvent);
                if (examEvent != null)
                {
                    return examEvent;
                }
            }

            if (timeManager.IsInflationShockDay(day))
            {
                var inflationEvent = PickPriorityMajor(nowAbsHour, day, IsInflationMajorEvent);
                if (inflationEvent != null)
                {
                    return inflationEvent;
                }
            }

            return PickEvent(majorEventPool, nowAbsHour, day);
        }

        public bool TryQueueMinorForCurrentDay()
        {
            if (minorQueuedDay == timeManager.Day)
            {
                return false;
            }

            if (eventManager.CurrentEvent != null || eventManager.HasPendingEvents)
            {
                return false;
            }

            var selectedEvent = PickMinorEventForDay(timeManager.Day, timeManager.Hour);
            if (selectedEvent == null)
            {
                minorQueuedDay = timeManager.Day;
                return false;
            }

            EnqueueWithCooldown(selectedEvent);
            minorQueuedDay = timeManager.Day;
            return true;
        }

        public bool TryQueueMajorForCurrentDay()
        {
            if (majorQueuedDay == timeManager.Day)
            {
                return false;
            }

            if (eventManager.CurrentEvent != null || eventManager.HasPendingEvents)
            {
                return false;
            }

            var selectedEvent = PickMajorEventForDay(timeManager.Day, timeManager.Hour);
            majorQueuedDay = timeManager.Day;
            if (selectedEvent == null)
            {
                return false;
            }

            EnqueueWithCooldown(selectedEvent);
            return true;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
            eventManager.OnChoiceApplied -= HandleChoiceApplied;
            eventManager.OnEventCompleted -= HandleEventCompleted;
        }

        private void HandleDayChanged(int _)
        {
            EnqueueDueFollowUps(timeManager.Day);
            DrainBufferedFollowUps();
            TryQueueMinorForCurrentDay();
        }

        private void PopulatePools(IEnumerable<EventData> events)
        {
            if (events == null)
            {
                return;
            }

            foreach (var eventData in events)
            {
                if (eventData == null)
                {
                    continue;
                }

                if (NormalizeCategory(eventData.Category) == MajorCategory)
                {
                    majorEventPool.Add(eventData);
                }
                else
                {
                    minorEventPool.Add(eventData);
                }

                var normalizedId = NormalizeId(eventData.EventId);
                if (!string.IsNullOrWhiteSpace(normalizedId) && !eventsById.ContainsKey(normalizedId))
                {
                    eventsById.Add(normalizedId, eventData);
                }
            }
        }

        private EventData PickEvent(IReadOnlyList<EventData> pool, int nowAbsHour, int day)
        {
            if (pool == null || pool.Count == 0)
            {
                return null;
            }

            eligibleEvents.Clear();
            for (var i = 0; i < pool.Count; i++)
            {
                var eventData = pool[i];
                if (!IsEligible(eventData, nowAbsHour, day))
                {
                    continue;
                }

                eligibleEvents.Add(eventData);
            }

            if (eligibleEvents.Count == 0)
            {
                return null;
            }

            return SelectWeightedRandomEvent(eligibleEvents);
        }

        private EventData PickPriorityMajor(int nowAbsHour, int day, Func<EventData, bool> predicate)
        {
            eligibleEvents.Clear();
            for (var i = 0; i < majorEventPool.Count; i++)
            {
                var eventData = majorEventPool[i];
                if (eventData == null || !predicate(eventData))
                {
                    continue;
                }

                if (!IsEligible(eventData, nowAbsHour, day))
                {
                    continue;
                }

                eligibleEvents.Add(eventData);
            }

            if (eligibleEvents.Count == 0)
            {
                return null;
            }

            return SelectWeightedRandomEvent(eligibleEvents);
        }

        private void EnqueueWithCooldown(EventData eventData)
        {
            TryEnqueueWithCooldown(eventData);
        }

        private bool TryEnqueueWithCooldown(EventData eventData)
        {
            if (eventData == null)
            {
                return false;
            }

            var nowAbsHour = GetAbsoluteHour(timeManager.Day, timeManager.Hour);
            var enqueued = eventManager.EnqueueEvent(eventData);
            if (!enqueued)
            {
                return false;
            }

            var cooldownHours = cooldownConfig.GetCooldownHours(eventData);
            eventCooldownUntilHour[GetEventKey(eventData)] = nowAbsHour + cooldownHours;
            return true;
        }

        private void HandleEventCompleted(EventData _)
        {
            DrainBufferedFollowUps();
        }

        private bool IsEligible(EventData eventData, int nowAbsHour, int day)
        {
            if (eventData == null)
            {
                return false;
            }

            var key = GetEventKey(eventData);
            if (eventCooldownUntilHour.TryGetValue(key, out var cooldownUntil)
                && nowAbsHour < cooldownUntil)
            {
                return false;
            }

            var availableChoices = eventManager.GetAvailableChoices(eventData);
            return availableChoices.Count > 0 && MatchesContext(eventData, day);
        }

        private bool MatchesContext(EventData eventData, int day)
        {
            if (eventData == null || eventData.RequiredContextTags == null || eventData.RequiredContextTags.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < eventData.RequiredContextTags.Count; i++)
            {
                if (!MatchesTag(eventData.RequiredContextTags[i], day))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesTag(EventContextTag tag, int day)
        {
            switch (tag)
            {
                case EventContextTag.ExamWindow:
                    return timeManager.IsInExamWindow(day);
                case EventContextTag.NotExamWindow:
                    return !timeManager.IsInExamWindow(day);
                case EventContextTag.InflationDay:
                    return timeManager.IsInflationShockDay(day);
                case EventContextTag.NotInflationDay:
                    return !timeManager.IsInflationShockDay(day);
                case EventContextTag.KykPayday:
                    return timeManager.IsKykPayday(day);
                case EventContextTag.FirstSemester:
                    return !timeManager.IsSecondSemester(day);
                case EventContextTag.SecondSemester:
                    return timeManager.IsSecondSemester(day);
                case EventContextTag.DebtPressureHigh:
                    return HasNumericFlagAtLeast("debt_pressure", 3f);
                case EventContextTag.WorkStrainHigh:
                    return HasNumericFlagAtLeast("work_strain", 2f);
                case EventContextTag.BurnoutHigh:
                    return HasNumericFlagAtLeast("burnout", 2f);
                case EventContextTag.KykRiskDaysHigh:
                    return HasNumericFlagAtLeast("kyk_risk_days", 2f);
                case EventContextTag.IllegalFinePending:
                    return HasNumericFlagAtLeast("illegal_fine_pending", 1f);
                case EventContextTag.KykStatusCut:
                    return HasTextFlag("kyk_status", "Cut");
            }

            if (statSystem == null)
            {
                // Stat-dependent context tags must fail closed when scheduler has no stat service.
                return false;
            }

            return tag switch
            {
                EventContextTag.MoneyLow => statSystem.GetStat(StatType.Money) < -200f,
                EventContextTag.MoneyCritical => statSystem.GetStat(StatType.Money) < -1000f,
                EventContextTag.MentalLow => statSystem.GetStat(StatType.Mental) < 40f,
                EventContextTag.EnergyLow => statSystem.GetStat(StatType.Energy) < 20f,
                EventContextTag.AcademicLow => statSystem.GetStat(StatType.Academic) < 2f,
                EventContextTag.AcademicHigh => statSystem.GetStat(StatType.Academic) >= 2.5f,
                _ => true
            };
        }

        private bool HasNumericFlagAtLeast(string key, float minValue)
        {
            if (flagStateService == null || string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return flagStateService.TryGetNumeric(key, out var value) && value >= minValue;
        }

        private bool HasTextFlag(string key, string expectedValue)
        {
            if (flagStateService == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(expectedValue))
            {
                return false;
            }

            return flagStateService.TryGetText(key, out var value)
                && string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
        }

        private string GetEventKey(EventData eventData)
        {
            if (eventData == null)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(eventData.EventId))
            {
                return eventData.EventId.Trim();
            }

            var instanceId = eventData.GetInstanceID();
            if (missingEventIdWarnings.Add(instanceId))
            {
                Debug.LogWarning($"[EventScheduler] Event '{eventData.name}' has empty EventId. Falling back to instance key.");
            }

            return "__instance_" + instanceId;
        }

        private static int GetAbsoluteHour(int day, int hour)
        {
            var normalizedDay = Math.Max(day, 1);
            var normalizedHour = Math.Clamp(hour, 0, 23);
            return (normalizedDay - 1) * 24 + normalizedHour;
        }

        private static EventData SelectWeightedRandomEvent(IReadOnlyList<EventData> candidates)
        {
            if (candidates == null || candidates.Count == 0)
            {
                return null;
            }

            var totalWeight = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                totalWeight += Mathf.Max(0f, candidates[i].SelectionWeight);
            }

            if (totalWeight <= 0f)
            {
                var randomIndex = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[randomIndex];
            }

            var roll = UnityEngine.Random.Range(0f, totalWeight);
            var cumulative = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                cumulative += Mathf.Max(0f, candidates[i].SelectionWeight);
                if (roll <= cumulative)
                {
                    return candidates[i];
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static string NormalizeCategory(string category)
        {
            return string.IsNullOrWhiteSpace(category)
                ? MinorCategory
                : category.Trim().ToLowerInvariant();
        }

        private static string NormalizeId(string eventId)
        {
            return string.IsNullOrWhiteSpace(eventId)
                ? string.Empty
                : eventId.Trim().ToLowerInvariant();
        }

        private static bool IsExamMajorEvent(EventData eventData)
        {
            return HasToken(eventData, "exam")
                || HasToken(eventData, "vize")
                || HasToken(eventData, "final");
        }

        private static bool IsInflationMajorEvent(EventData eventData)
        {
            return HasToken(eventData, "inflation")
                || HasToken(eventData, "enflasyon");
        }

        private static bool HasToken(EventData eventData, string token)
        {
            if (eventData == null || string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            return ContainsInvariant(eventData.EventId, token)
                || ContainsInvariant(eventData.Title, token)
                || ContainsInvariant(eventData.Category, token);
        }

        private static bool ContainsInvariant(string source, string token)
        {
            return !string.IsNullOrWhiteSpace(source)
                && source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}

