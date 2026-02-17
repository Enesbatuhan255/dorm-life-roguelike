using System.Collections.Generic;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventSchedulerPolicyTests
    {
        private readonly List<EventData> createdEvents = new List<EventData>();

        [TearDown]
        public void TearDown()
        {
            EventTestFactory.Destroy(createdEvents.ToArray());
            createdEvents.Clear();
        }

        [Test]
        public void DayStart_QueuesMinor_EndDayQueuesMajor()
        {
            var time = new TimeManager();
            var manager = new EventManager(new StatSystem(), time);
            var minor = Track(EventTestFactory.CreateEvent("EVT_MINOR_TEST", true, category: "Minor"));
            var major = Track(EventTestFactory.CreateEvent("EVT_MAJOR_TEST", true, category: "Major"));

            using var scheduler = new EventScheduler(
                time,
                manager,
                new[] { minor, major },
                checkIntervalHours: 1,
                cooldownHours: 0);

            Assert.That(manager.CurrentEvent, Is.SameAs(minor));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.Null);

            var queued = scheduler.TryQueueMajorForCurrentDay();
            Assert.That(queued, Is.True);
            Assert.That(manager.CurrentEvent, Is.SameAs(major));
        }

        [Test]
        public void ExamWindow_PrioritizesExamMajor()
        {
            var time = new TimeManager();
            var manager = new EventManager(new StatSystem(), time);
            var genericMajor = Track(EventTestFactory.CreateEvent("EVT_MAJOR_GENERIC", true, selectionWeight: 100f, category: "Major"));
            var examMajor = Track(EventTestFactory.CreateEvent("EVT_MAJOR_EXAM_VIZE", true, selectionWeight: 1f, category: "Major"));

            using var scheduler = new EventScheduler(
                time,
                manager,
                new[] { genericMajor, examMajor },
                checkIntervalHours: 1,
                cooldownHours: 0);

            time.AdvanceTime((16 - 1) * 24);
            var picked = scheduler.PickMajorEventForDay(time.Day, time.Hour);
            Assert.That(picked, Is.SameAs(examMajor));
        }

        [Test]
        public void InflationDay_PrioritizesInflationMajor()
        {
            var time = new TimeManager();
            var manager = new EventManager(new StatSystem(), time);
            var genericMajor = Track(EventTestFactory.CreateEvent("EVT_MAJOR_GENERIC_2", true, selectionWeight: 100f, category: "Major"));
            var inflationMajor = Track(EventTestFactory.CreateEvent("EVT_MAJOR_INFLATION_SHOCK", true, selectionWeight: 1f, category: "Major"));

            using var scheduler = new EventScheduler(
                time,
                manager,
                new[] { genericMajor, inflationMajor },
                checkIntervalHours: 1,
                cooldownHours: 0);

            time.AdvanceTime((37 - 1) * 24);
            var picked = scheduler.PickMajorEventForDay(time.Day, time.Hour);
            Assert.That(picked, Is.SameAs(inflationMajor));
        }

        private EventData Track(EventData eventData)
        {
            createdEvents.Add(eventData);
            return eventData;
        }

        [Test]
        public void MinorSelection_SkipsMoneyLowTaggedEvent_WhenMoneyIsNotLow()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Money, 50f);
            var manager = new EventManager(stats, time);
            var moneyLowMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_MONEY_LOW",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.MoneyLow }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_GENERIC",
                true,
                selectionWeight: 1f,
                category: "Minor"));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { moneyLowMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            var picked = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(picked, Is.SameAs(genericMinor));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void MinorSelection_AllowsMoneyLowTaggedEvent_WhenMoneyIsLow()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Money, -500f);
            var manager = new EventManager(stats, time);
            var moneyLowMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_MONEY_LOW_2",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.MoneyLow }));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { moneyLowMinor },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            var picked = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(picked, Is.SameAs(moneyLowMinor));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void MinorSelection_ExamWindowTag_OnlyEligibleInExamWindow()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var examTaggedMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_EXAM_ONLY",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.ExamWindow }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_NON_EXAM",
                true,
                selectionWeight: 1f,
                category: "Minor"));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { examTaggedMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            var normalDayPick = scheduler.PickMinorEventForDay(10, 8);
            Assert.That(normalDayPick, Is.SameAs(genericMinor));

            var examDayPick = scheduler.PickMinorEventForDay(16, 8);
            Assert.That(examDayPick, Is.SameAs(examTaggedMinor));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void CompletedEvent_EnqueuesConfiguredFollowUp()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var followUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_FOLLOWUP", true, category: "Minor"));
            var root = Track(EventTestFactory.CreateEvent(
                "EVT_CHAIN_ROOT",
                true,
                category: "Minor",
                followUpEventIds: new[] { "EVT_CHAIN_FOLLOWUP" }));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, followUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.SameAs(followUp));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void ChoiceFollowUp_TakesPrecedenceOverEventFollowUp()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var choiceFollowUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_CHOICE", true, category: "Minor"));
            var eventFollowUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_EVENT", true, category: "Minor"));
            var root = Track(EventTestFactory.CreateEvent(
                "EVT_CHAIN_ROOT_SELECTIVE",
                true,
                category: "Minor",
                followUpEventIds: new[] { "EVT_CHAIN_EVENT" },
                firstChoiceFollowUpEventIds: new[] { "EVT_CHAIN_CHOICE" }));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, eventFollowUp, choiceFollowUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.SameAs(choiceFollowUp));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void ChoiceWithoutFollowUp_DoesNotTriggerChain()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var followUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_OPTIONAL", true, category: "Minor"));
            var root = Track(EventTestFactory.CreateEventWithChoices(
                "EVT_CHAIN_OPTIONAL_ROOT",
                "Minor",
                new[] { "EVT_CHAIN_OPTIONAL" },
                new string[0]));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, followUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 1, out _);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void CompletedEvent_MissingFollowUp_DoesNotBreakFlow()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var root = Track(EventTestFactory.CreateEvent(
                "EVT_CHAIN_ROOT_MISS",
                true,
                category: "Minor",
                followUpEventIds: new[] { "EVT_CHAIN_NOT_FOUND" }));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            Object.DestroyImmediate(cooldownConfig);
        }
    }
}
