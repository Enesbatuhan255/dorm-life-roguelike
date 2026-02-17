using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventSchedulerPolicyTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
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
        public void MinorSelection_StatTaggedEvent_IsNotEligible_WhenSchedulerHasNoStatSystem()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Money, -500f);
            var manager = new EventManager(stats, time);
            var moneyLowMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_MONEY_LOW_NO_STATS",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.MoneyLow }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_GENERIC_NO_STATS",
                true,
                selectionWeight: 1f,
                category: "Minor"));

            using var scheduler = new EventScheduler(
                time,
                manager,
                new[] { moneyLowMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownHours: 0);

            var picked = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(picked, Is.SameAs(genericMinor));
        }

        [Test]
        public void MinorSelection_TimeTaggedEvent_WorksWithoutStatSystem()
        {
            var time = new TimeManager();
            var manager = new EventManager(new StatSystem(), time);
            var examTaggedMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_EXAM_NO_STATS",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.ExamWindow }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_NON_EXAM_NO_STATS",
                true,
                selectionWeight: 1f,
                category: "Minor"));

            using var scheduler = new EventScheduler(
                time,
                manager,
                new[] { examTaggedMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownHours: 0);

            var normalDayPick = scheduler.PickMinorEventForDay(10, 8);
            Assert.That(normalDayPick, Is.SameAs(genericMinor));

            var examDayPick = scheduler.PickMinorEventForDay(16, 8);
            Assert.That(examDayPick, Is.SameAs(examTaggedMinor));
        }

        [Test]
        public void MinorSelection_DebtPressureTag_OnlyEligibleWhenPressureHigh()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var flags = new FlagStateService();
            var manager = new EventManager(stats, time, flags);
            var debtTaggedMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_DEBT_TAGGED",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.DebtPressureHigh }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_DEBT_GENERIC",
                true,
                selectionWeight: 0f,
                category: "Minor"));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { debtTaggedMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig,
                flagStateService: flags);

            var beforePressure = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(beforePressure, Is.SameAs(genericMinor));

            flags.ApplyChanges(new[] { CreateNumericFlag("debt_pressure", 3f) });
            var afterPressure = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(afterPressure, Is.SameAs(debtTaggedMinor));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void MinorSelection_KykStatusCutTag_OnlyEligibleWhenStatusCut()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var flags = new FlagStateService();
            var manager = new EventManager(stats, time, flags);
            var cutTaggedMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_KYK_CUT_TAGGED",
                true,
                selectionWeight: 100f,
                category: "Minor",
                requiredContextTags: new[] { EventContextTag.KykStatusCut }));
            var genericMinor = Track(EventTestFactory.CreateEvent(
                "EVT_MINOR_KYK_GENERIC",
                true,
                selectionWeight: 0f,
                category: "Minor"));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { cutTaggedMinor, genericMinor },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig,
                flagStateService: flags);

            var beforeCut = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(beforeCut, Is.SameAs(genericMinor));

            flags.ApplyChanges(new[] { CreateTextFlag("kyk_status", "Cut") });
            var afterCut = scheduler.PickMinorEventForDay(time.Day, time.Hour);
            Assert.That(afterCut, Is.SameAs(cutTaggedMinor));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void CompletedEvent_EnqueuesConfiguredFollowUp()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var followUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_FOLLOWUP", true, category: "Major"));
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
            var choiceFollowUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_CHOICE", true, category: "Major"));
            var eventFollowUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_EVENT", true, category: "Major"));
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
            var followUp = Track(EventTestFactory.CreateEvent("EVT_CHAIN_OPTIONAL", true, category: "Major"));
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
        public void ChoiceFollowUp_WithDelay_EnqueuesAfterSpecifiedDays()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var outcomes = new List<string>();
            manager.OnOutcomeLogged += outcomes.Add;
            var followUp = Track(EventTestFactory.CreateEvent("EVT_DELAYED_CHOICE_FOLLOWUP", true, category: "Major"));
            var root = Track(EventTestFactory.CreateEvent(
                "EVT_DELAYED_CHOICE_ROOT",
                true,
                category: "Minor",
                firstChoiceFollowUpEventIds: new[] { "EVT_DELAYED_CHOICE_FOLLOWUP" },
                firstChoiceFollowUpDelayDays: 2));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(999);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, followUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);
            Assert.That(outcomes, Has.Some.Contains("hemen etkisini gostermeyebilir"));

            time.AdvanceTime(24);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            time.AdvanceTime(24);
            Assert.That(manager.CurrentEvent, Is.SameAs(followUp));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void EventFollowUp_WithDelay_EnqueuesAfterSpecifiedDays()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var punish = Track(EventTestFactory.CreateEvent("EVT_DELAYED_EVENT_PUNISH", true, category: "Major"));
            var chase = Track(EventTestFactory.CreateEvent(
                "EVT_DELAYED_EVENT_CHASE",
                true,
                category: "Major",
                followUpEventIds: new[] { "EVT_DELAYED_EVENT_PUNISH" },
                followUpDelayDays: 1));
            var root = Track(EventTestFactory.CreateEvent(
                "EVT_DELAYED_EVENT_ROOT",
                true,
                category: "Minor",
                firstChoiceFollowUpEventIds: new[] { "EVT_DELAYED_EVENT_CHASE" },
                firstChoiceFollowUpDelayDays: 2));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(999);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { root, chase, punish },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            Assert.That(manager.CurrentEvent, Is.SameAs(root));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.Null);

            time.AdvanceTime(48);
            Assert.That(manager.CurrentEvent, Is.SameAs(chase));

            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);

            time.AdvanceTime(24);
            Assert.That(manager.CurrentEvent, Is.SameAs(punish));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void MultipleSources_SameImmediateFollowUp_ProcessesEachTrigger()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var completedIds = new List<string>();
            manager.OnEventCompleted += e => completedIds.Add(e.EventId);

            var repeatedFollowUp = Track(EventTestFactory.CreateEvent("EVT_REPEAT_IMMEDIATE", true, category: "Major"));
            var rootA = Track(EventTestFactory.CreateEvent(
                "EVT_ROOT_A",
                true,
                selectionWeight: 100f,
                category: "Minor",
                followUpEventIds: new[] { "EVT_REPEAT_IMMEDIATE" }));
            var rootB = Track(EventTestFactory.CreateEvent(
                "EVT_ROOT_B",
                true,
                selectionWeight: 0f,
                category: "Minor",
                followUpEventIds: new[] { "EVT_REPEAT_IMMEDIATE" }));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { rootA, rootB, repeatedFollowUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            manager.EnqueueEvent(rootB);

            while (manager.CurrentEvent != null)
            {
                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            }

            Assert.That(completedIds.FindAll(id => id == "EVT_REPEAT_IMMEDIATE").Count, Is.EqualTo(2));

            Object.DestroyImmediate(cooldownConfig);
        }

        [Test]
        public void MultipleSources_SameDelayedFollowUp_ProcessesEachTrigger()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var manager = new EventManager(stats, time);
            var completedIds = new List<string>();
            manager.OnEventCompleted += e => completedIds.Add(e.EventId);

            var repeatedFollowUp = Track(EventTestFactory.CreateEvent("EVT_REPEAT_DELAYED", true, category: "Major"));
            var rootA = Track(EventTestFactory.CreateEvent(
                "EVT_ROOT_DELAY_A",
                true,
                selectionWeight: 100f,
                category: "Minor",
                followUpEventIds: new[] { "EVT_REPEAT_DELAYED" },
                followUpDelayDays: 1));
            var rootB = Track(EventTestFactory.CreateEvent(
                "EVT_ROOT_DELAY_B",
                true,
                selectionWeight: 0f,
                category: "Minor",
                followUpEventIds: new[] { "EVT_REPEAT_DELAYED" },
                followUpDelayDays: 1));
            var cooldownConfig = ScriptableObject.CreateInstance<EventCooldownConfig>();
            cooldownConfig.SetRuntimeDefaults(0);

            using var scheduler = new EventScheduler(
                time,
                manager,
                stats,
                new[] { rootA, rootB, repeatedFollowUp },
                checkIntervalHours: 1,
                cooldownConfig: cooldownConfig);

            manager.EnqueueEvent(rootB);

            while (manager.CurrentEvent != null)
            {
                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            }

            time.AdvanceTime(24);

            while (manager.CurrentEvent != null)
            {
                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            }

            Assert.That(completedIds.FindAll(id => id == "EVT_REPEAT_DELAYED").Count, Is.EqualTo(2));

            Object.DestroyImmediate(cooldownConfig);
        }

        private static EventFlagChange CreateNumericFlag(string key, float value)
        {
            var flag = new EventFlagChange();
            SetField(flag, "key", key);
            SetField(flag, "mode", EventFlagChangeMode.AddNumeric);
            SetField(flag, "numericValue", value);
            SetField(flag, "textValue", string.Empty);
            return flag;
        }

        private static EventFlagChange CreateTextFlag(string key, string value)
        {
            var flag = new EventFlagChange();
            SetField(flag, "key", key);
            SetField(flag, "mode", EventFlagChangeMode.SetText);
            SetField(flag, "numericValue", 0f);
            SetField(flag, "textValue", value);
            return flag;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
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

