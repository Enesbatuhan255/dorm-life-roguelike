using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public enum EventContextTag
    {
        MoneyLow,
        MoneyCritical,
        MentalLow,
        EnergyLow,
        AcademicLow,
        AcademicHigh,
        ExamWindow,
        NotExamWindow,
        InflationDay,
        NotInflationDay,
        KykPayday,
        FirstSemester,
        SecondSemester
    }

    [CreateAssetMenu(fileName = "EventData", menuName = "DormLifeRoguelike/Event Data")]
    public sealed class EventData : ScriptableObject
    {
        [Tooltip("Stable ID for queue dedupe/cooldown. Example: EVT_DORM_001")]
        [SerializeField] private string eventId = string.Empty;
        [SerializeField] private string title = "Event Title";
        [SerializeField] private string description = "Event Description";
        [SerializeField] private string category = "Minor";
        [Min(0f)]
        [SerializeField] private float selectionWeight = 1f;
        [SerializeField] private List<EventContextTag> requiredContextTags = new List<EventContextTag>();
        [Tooltip("Optional follow-up event IDs enqueued after this event is completed.")]
        [SerializeField] private List<string> followUpEventIds = new List<string>();
        [SerializeField] private List<EventChoice> choices = new List<EventChoice>();

        public string EventId => eventId;

        public string Title => title;

        public string Description => description;

        public string Category => category;

        public float SelectionWeight => selectionWeight;

        public IReadOnlyList<EventContextTag> RequiredContextTags => requiredContextTags;

        public IReadOnlyList<string> FollowUpEventIds => followUpEventIds;

        public IReadOnlyList<EventChoice> Choices => choices;

        private void OnValidate()
        {
            var trimmed = eventId == null ? string.Empty : eventId.Trim();
            if (!string.Equals(eventId, trimmed))
            {
                eventId = trimmed;
            }

            if (string.IsNullOrWhiteSpace(eventId))
            {
                Debug.LogWarning($"[EventData] '{name}' has empty eventId. Use a stable ID like EVT_DORM_001.", this);
                return;
            }

            if (!eventId.StartsWith("EVT_"))
            {
                Debug.LogWarning($"[EventData] '{name}' eventId '{eventId}' should start with 'EVT_'.", this);
            }

            for (var i = 0; i < followUpEventIds.Count; i++)
            {
                var id = followUpEventIds[i];
                var trimmedFollowUpId = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();
                if (!string.Equals(id, trimmedFollowUpId))
                {
                    followUpEventIds[i] = trimmedFollowUpId;
                }
            }
        }
    }
}
