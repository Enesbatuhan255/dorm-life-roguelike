using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public static class EventTestFactory
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        public static EventData CreateEvent(
            string eventId,
            bool withSingleChoice,
            float selectionWeight = 1f,
            string category = "General",
            EventContextTag[] requiredContextTags = null,
            string[] followUpEventIds = null,
            string[] firstChoiceFollowUpEventIds = null,
            int followUpDelayDays = 0,
            int firstChoiceFollowUpDelayDays = 0)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "title", eventId);
            SetField(eventData, "description", "test");
            SetField(eventData, "category", category ?? "General");
            SetField(eventData, "selectionWeight", selectionWeight);
            SetField(eventData, "requiredContextTags", new List<EventContextTag>(requiredContextTags ?? new EventContextTag[0]));
            SetField(eventData, "followUpEventIds", new List<string>(followUpEventIds ?? new string[0]));
            SetField(eventData, "followUpDelayDays", followUpDelayDays);

            var choices = new List<EventChoice>();
            if (withSingleChoice)
            {
                var choice = new EventChoice();
                SetField(choice, "followUpEventIds", new List<string>(firstChoiceFollowUpEventIds ?? new string[0]));
                SetField(choice, "followUpDelayDays", firstChoiceFollowUpDelayDays);
                choices.Add(choice);
            }

            SetField(eventData, "choices", choices);
            return eventData;
        }

        public static void Destroy(params EventData[] assets)
        {
            if (assets == null)
            {
                return;
            }

            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null)
                {
                    Object.DestroyImmediate(assets[i]);
                }
            }
        }

        public static EventData CreateEventWithChoices(
            string eventId,
            string category,
            params string[][] choiceFollowUpEventIds)
        {
            return CreateEventWithChoices(eventId, category, choiceFollowUpEventIds, null);
        }

        public static EventData CreateEventWithChoices(
            string eventId,
            string category,
            string[][] choiceFollowUpEventIds,
            int[] choiceFollowUpDelayDays)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "title", eventId);
            SetField(eventData, "description", "test");
            SetField(eventData, "category", category ?? "General");
            SetField(eventData, "selectionWeight", 1f);
            SetField(eventData, "requiredContextTags", new List<EventContextTag>());
            SetField(eventData, "followUpEventIds", new List<string>());

            var choices = new List<EventChoice>();
            if (choiceFollowUpEventIds != null)
            {
                for (var i = 0; i < choiceFollowUpEventIds.Length; i++)
                {
                    var choice = new EventChoice();
                    SetField(choice, "text", $"Choice_{i}");
                    SetField(choice, "followUpEventIds", new List<string>(choiceFollowUpEventIds[i] ?? new string[0]));
                    var choiceDelayDays = choiceFollowUpDelayDays != null && i < choiceFollowUpDelayDays.Length
                        ? choiceFollowUpDelayDays[i]
                        : 0;
                    SetField(choice, "followUpDelayDays", choiceDelayDays);
                    choices.Add(choice);
                }
            }

            SetField(eventData, "choices", choices);
            return eventData;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
        }
    }
}
