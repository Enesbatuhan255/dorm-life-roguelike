using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class EventManager : IEventManager
    {
        private const int MaxPendingQueueSize = 5;

        private readonly IStatSystem statSystem;
        private readonly ITimeManager timeManager;
        private readonly IFlagStateService flagStateService;
        private readonly Queue<EventData> pendingEvents = new Queue<EventData>();
        private readonly HashSet<string> pendingEventIds = new HashSet<string>();
        private readonly HashSet<int> missingEventIdWarnings = new HashSet<int>();
        private EventData currentEvent;

        public EventManager(IStatSystem statSystem, ITimeManager timeManager)
            : this(statSystem, timeManager, null)
        {
        }

        public EventManager(IStatSystem statSystem, ITimeManager timeManager, IFlagStateService flagStateService)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.flagStateService = flagStateService;
        }

        public event Action<EventData> OnEventStarted;
        public event Action<EventData> OnEventCompleted;
        public event Action<EventData, EventChoice> OnChoiceApplied;
        public event Action<string> OnOutcomeLogged;

        public EventData CurrentEvent => currentEvent;

        public bool HasPendingEvents => pendingEvents.Count > 0;

        public bool EnqueueEvent(EventData eventData)
        {
            if (eventData == null)
            {
                LogOutcome("Cannot enqueue null event data.");
                return false;
            }

            var eventKey = GetEventKey(eventData);
            if (currentEvent != null && GetEventKey(currentEvent) == eventKey)
            {
                LogOutcome($"Skipped duplicate enqueue for active event '{eventData.Title}'.");
                return false;
            }

            if (pendingEventIds.Contains(eventKey))
            {
                LogOutcome($"Skipped duplicate enqueue for pending event '{eventData.Title}'.");
                return false;
            }

            if (pendingEvents.Count >= MaxPendingQueueSize)
            {
                LogOutcome($"Dropped event '{eventData.Title}' because pending queue is full ({MaxPendingQueueSize}).");
                return false;
            }

            pendingEvents.Enqueue(eventData);
            pendingEventIds.Add(eventKey);
            TryStartNextEvent();
            return true;
        }

        public IReadOnlyList<EventChoice> GetAvailableChoices(EventData eventData)
        {
            if (eventData == null)
            {
                return Array.Empty<EventChoice>();
            }

            var availableChoices = new List<EventChoice>();
            var allChoices = eventData.Choices;

            for (var i = 0; i < allChoices.Count; i++)
            {
                var choice = allChoices[i];
                if (choice != null && IsConditionMet(choice.Condition))
                {
                    availableChoices.Add(choice);
                }
            }

            return availableChoices;
        }

        public void PublishSystemMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            LogOutcome(message.Trim());
        }

        public bool TryApplyChoice(EventData eventData, int choiceIndex, out string outcomeMessage)
        {
            outcomeMessage = string.Empty;

            if (eventData == null)
            {
                outcomeMessage = "Event data is null.";
                LogOutcome(outcomeMessage);
                return false;
            }

            var choices = eventData.Choices;
            if (choiceIndex < 0 || choiceIndex >= choices.Count)
            {
                outcomeMessage = $"Invalid choice index: {choiceIndex}.";
                LogOutcome(outcomeMessage);
                return false;
            }

            var choice = choices[choiceIndex];
            if (choice == null)
            {
                outcomeMessage = $"Choice at index {choiceIndex} is null.";
                LogOutcome(outcomeMessage);
                return false;
            }

            if (!IsConditionMet(choice.Condition))
            {
                outcomeMessage = $"Choice '{choice.Text}' does not meet its condition.";
                LogOutcome(outcomeMessage);
                return false;
            }

            var effects = choice.Effects;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                {
                    continue;
                }

                statSystem.ApplyBaseDelta(effect.StatType, effect.Delta);
            }

            flagStateService?.ApplyChanges(choice.Flags);

            var timeAdvanceHours = Mathf.Max(0, choice.TimeAdvanceHours);
            if (timeAdvanceHours > 0)
            {
                timeManager.AdvanceTime(timeAdvanceHours);
            }

            outcomeMessage = $"Applied choice '{choice.Text}' for event '{eventData.Title}'.";
            LogOutcome(outcomeMessage);
            OnChoiceApplied?.Invoke(eventData, choice);

            if (ReferenceEquals(eventData, currentEvent))
            {
                CompleteCurrentEvent();
            }

            return true;
        }

        public EventManagerSnapshot CaptureRuntimeSnapshot()
        {
            var snapshot = new EventManagerSnapshot
            {
                currentEventId = NormalizeEventId(currentEvent != null ? currentEvent.EventId : string.Empty)
            };

            foreach (var pending in pendingEvents)
            {
                if (pending == null)
                {
                    continue;
                }

                var eventId = NormalizeEventId(pending.EventId);
                if (!string.IsNullOrWhiteSpace(eventId))
                {
                    snapshot.pendingEventIds.Add(eventId);
                }
            }

            return snapshot;
        }

        public void RestoreRuntimeSnapshot(EventManagerSnapshot snapshot, IReadOnlyDictionary<string, EventData> eventLookup)
        {
            pendingEvents.Clear();
            pendingEventIds.Clear();
            currentEvent = null;

            if (snapshot == null || eventLookup == null || eventLookup.Count == 0)
            {
                return;
            }

            var currentId = NormalizeEventId(snapshot.currentEventId);
            if (TryResolveEventById(currentId, eventLookup, out var restoredCurrent) && restoredCurrent != null)
            {
                currentEvent = restoredCurrent;
            }

            var pendingIds = snapshot.pendingEventIds;
            if (pendingIds != null)
            {
                for (var i = 0; i < pendingIds.Count; i++)
                {
                    var pendingId = NormalizeEventId(pendingIds[i]);
                    if (string.IsNullOrWhiteSpace(pendingId) || pendingId == currentId)
                    {
                        continue;
                    }

                    if (!TryResolveEventById(pendingId, eventLookup, out var pendingEvent) || pendingEvent == null)
                    {
                        continue;
                    }

                    var key = GetEventKey(pendingEvent);
                    if (pendingEventIds.Add(key))
                    {
                        pendingEvents.Enqueue(pendingEvent);
                    }
                }
            }

            if (currentEvent != null)
            {
                OnEventStarted?.Invoke(currentEvent);
                LogOutcome($"Event restored: '{currentEvent.Title}'.");
                return;
            }

            TryStartNextEvent();
        }

        private void TryStartNextEvent()
        {
            if (currentEvent != null || pendingEvents.Count == 0)
            {
                return;
            }

            currentEvent = pendingEvents.Dequeue();
            pendingEventIds.Remove(GetEventKey(currentEvent));
            OnEventStarted?.Invoke(currentEvent);
            LogOutcome($"Event started: '{currentEvent.Title}'.");
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
                Debug.LogWarning($"[EventManager] Event '{eventData.name}' has empty EventId. Falling back to instance key.");
            }

            return "__instance_" + instanceId;
        }

        private void CompleteCurrentEvent()
        {
            if (currentEvent == null)
            {
                return;
            }

            var completed = currentEvent;
            currentEvent = null;

            OnEventCompleted?.Invoke(completed);
            LogOutcome($"Event completed: '{completed.Title}'.");

            TryStartNextEvent();
        }

        private bool IsConditionMet(EventCondition condition)
        {
            if (condition == null || !condition.IsEnabled)
            {
                return true;
            }

            var current = statSystem.GetStat(condition.StatType);
            var target = condition.Value;

            switch (condition.Operator)
            {
                case ConditionOperator.GreaterThan:
                    return current > target;
                case ConditionOperator.GreaterThanOrEqual:
                    return current >= target;
                case ConditionOperator.LessThan:
                    return current < target;
                case ConditionOperator.LessThanOrEqual:
                    return current <= target;
                case ConditionOperator.Equal:
                    return Mathf.Approximately(current, target);
                case ConditionOperator.NotEqual:
                    return !Mathf.Approximately(current, target);
                default:
                    return false;
            }
        }

        private void LogOutcome(string message)
        {
            Debug.Log($"[EventManager] {message}");
            OnOutcomeLogged?.Invoke(message);
        }

        private static string NormalizeEventId(string eventId)
        {
            return string.IsNullOrWhiteSpace(eventId) ? string.Empty : eventId.Trim();
        }

        private static bool TryResolveEventById(string eventId, IReadOnlyDictionary<string, EventData> eventLookup, out EventData eventData)
        {
            eventData = null;
            if (string.IsNullOrWhiteSpace(eventId) || eventLookup == null)
            {
                return false;
            }

            if (eventLookup.TryGetValue(eventId, out eventData))
            {
                return eventData != null;
            }

            return eventLookup.TryGetValue(eventId.ToLowerInvariant(), out eventData) && eventData != null;
        }
    }
}
