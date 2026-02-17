using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.PlayMode
{
    public sealed class EventSchedulerPlayModeCoverageTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<EventData> createdEvents = new List<EventData>();
        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (var i = 0; i < createdEvents.Count; i++)
            {
                if (createdEvents[i] != null)
                {
                    Object.DestroyImmediate(createdEvents[i]);
                }
            }

            createdEvents.Clear();

            for (var i = 0; i < createdObjects.Count; i++)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void DelayedChoiceFollowUp_EnqueuesAfterDelayDays()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var cooldown = TrackObject(EventCooldownConfig.CreateRuntimeDefault(0));

            var followUp = TrackEvent(CreateEvent("EVT_PM_DELAYED_FOLLOWUP", "Major"));
            var root = TrackEvent(CreateEvent(
                "EVT_PM_DELAYED_ROOT",
                "Major",
                choiceFollowUpEventIds: new[] { "EVT_PM_DELAYED_FOLLOWUP" },
                choiceFollowUpDelayDays: 2));

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, followUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldown);

            Assert.That(manager.EnqueueEvent(root), Is.True);
            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            Assert.That(manager.TryApplyChoice(root, 0, out _), Is.True);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            time.AdvanceTime(24);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            time.AdvanceTime(24);
            Assert.That(manager.CurrentEvent, Is.SameAs(followUp));
        }

        [Test]
        public void ChainRepeat_SameImmediateFollowUp_ProcessesEachTrigger()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var cooldown = TrackObject(EventCooldownConfig.CreateRuntimeDefault(0));
            var completedIds = new List<string>();
            manager.OnEventCompleted += eventData => completedIds.Add(eventData.EventId);

            var repeated = TrackEvent(CreateEvent("EVT_PM_REPEAT_TARGET", "Major"));
            var rootA = TrackEvent(CreateEvent(
                "EVT_PM_ROOT_A",
                "Minor",
                selectionWeight: 100f,
                eventFollowUpEventIds: new[] { "EVT_PM_REPEAT_TARGET" }));
            var rootB = TrackEvent(CreateEvent(
                "EVT_PM_ROOT_B",
                "Minor",
                selectionWeight: 0f,
                eventFollowUpEventIds: new[] { "EVT_PM_REPEAT_TARGET" }));

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { rootA, rootB, repeated },
                checkIntervalHours: 1,
                cooldownConfig: cooldown);

            Assert.That(manager.EnqueueEvent(rootB), Is.True);

            while (manager.CurrentEvent != null)
            {
                Assert.That(manager.TryApplyChoice(manager.CurrentEvent, 0, out _), Is.True);
            }

            Assert.That(completedIds.FindAll(id => id == "EVT_PM_REPEAT_TARGET").Count, Is.EqualTo(2));
        }

        [Test]
        public void ContextTags_RequireExamWindowAndMoneyLow()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Money, 50f);
            var manager = new EventManager(stats, time);
            var cooldown = TrackObject(EventCooldownConfig.CreateRuntimeDefault(0));

            var tagged = TrackEvent(CreateEvent(
                "EVT_PM_TAGGED",
                "Minor",
                selectionWeight: 100f,
                requiredTags: new[] { EventContextTag.ExamWindow, EventContextTag.MoneyLow }));
            var generic = TrackEvent(CreateEvent(
                "EVT_PM_GENERIC",
                "Minor",
                selectionWeight: 1f));

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { tagged, generic },
                checkIntervalHours: 1,
                cooldownConfig: cooldown);

            var examButMoneyNotLow = scheduler.PickMinorEventForDay(16, 8);
            Assert.That(examButMoneyNotLow, Is.SameAs(generic));

            stats.SetBaseValue(StatType.Money, -500f);

            var moneyLowButNotExam = scheduler.PickMinorEventForDay(10, 8);
            Assert.That(moneyLowButNotExam, Is.SameAs(generic));

            var bothTrue = scheduler.PickMinorEventForDay(16, 8);
            Assert.That(bothTrue, Is.SameAs(tagged));
        }

        private EventData TrackEvent(EventData eventData)
        {
            createdEvents.Add(eventData);
            return eventData;
        }

        private T TrackObject<T>(T obj)
            where T : Object
        {
            createdObjects.Add(obj);
            return obj;
        }

        private static EventData CreateEvent(
            string eventId,
            string category,
            float selectionWeight = 1f,
            EventContextTag[] requiredTags = null,
            string[] eventFollowUpEventIds = null,
            string[] choiceFollowUpEventIds = null,
            int choiceFollowUpDelayDays = 0)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "title", eventId);
            SetField(eventData, "description", "playmode coverage");
            SetField(eventData, "category", category);
            SetField(eventData, "selectionWeight", selectionWeight);
            SetField(eventData, "requiredContextTags", new List<EventContextTag>(requiredTags ?? new EventContextTag[0]));
            SetField(eventData, "followUpEventIds", new List<string>(eventFollowUpEventIds ?? new string[0]));
            SetField(eventData, "followUpDelayDays", 0);

            var choice = new EventChoice();
            SetField(choice, "text", "Choice");
            SetField(choice, "followUpEventIds", new List<string>(choiceFollowUpEventIds ?? new string[0]));
            SetField(choice, "followUpDelayDays", choiceFollowUpDelayDays);

            SetField(eventData, "choices", new List<EventChoice> { choice });
            return eventData;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
        }
    }
}
