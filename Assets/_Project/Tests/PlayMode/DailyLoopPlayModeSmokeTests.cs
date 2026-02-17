using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.PlayMode
{
    public sealed class DailyLoopPlayModeSmokeTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void MinorActionMajorDayEnd_AdvancesDay()
        {
            var createdEvents = new List<EventData>();
            var createdObjects = new List<Object>();

            try
            {
                var time = new TimeManager();
                var stats = new StatSystem();
                var sleepDebtConfig = TrackObject(createdObjects, SleepDebtConfig.CreateRuntimeDefault());
                var mentalConfig = TrackObject(createdObjects, MentalConfig.CreateRuntimeDefault());
                var outcomeConfig = TrackObject(createdObjects, ScriptableObject.CreateInstance<GameOutcomeConfig>());
                outcomeConfig.SetRuntimeValues(120, 0f, false, 0f, 0f, false, 0f, -99999f, GameOutcomeFailPriority.AcademicFirst);
                var academicConfig = TrackObject(createdObjects, AcademicConfig.CreateRuntimeDefault());
                var endingDatabase = TrackObject(createdObjects, CreatePopulatedEndingDatabase());

                var manager = new EventManager(stats, time);
                using var economy = new EconomySystem(stats, time);
                using var sleepDebt = new SleepDebtSystem(time, stats, sleepDebtConfig);
                using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
                var challengeConfig = TrackObject(createdObjects, MicroChallengeConfig.CreateRuntimeDefault());
                var challengeService = new MicroChallengeService(stats, time, economy, challengeConfig, () => 0.6f);

                var minor = TrackEvent(createdEvents, CreateEvent("EVT_PM_LOOP_MINOR", "Minor"));
                var major = TrackEvent(createdEvents, CreateEvent("EVT_PM_LOOP_MAJOR", "Major"));

                using var scheduler = new EventScheduler(time, manager, new[] { minor, major }, 1, 0);
                using var actions = new PlayerActionService(stats, time, sleepDebt, economy, outcome, scheduler, manager, null, mentalConfig, null);
                var planner = new DayPlanningService(actions, challengeService);

                Assert.That(manager.CurrentEvent, Is.SameAs(minor));
                Assert.That(manager.TryApplyChoice(manager.CurrentEvent, 0, out _), Is.True);

                Assert.That(planner.TryAddBlock(PlannedActionType.Study), Is.True);
                var planResult = planner.ExecutePlan();
                Assert.That(planResult.ExecutedBlocks, Is.EqualTo(1));
                Assert.That(planResult.Notes.Count, Is.GreaterThan(0));

                var dayBeforeEnd = time.Day;
                Assert.That(actions.TryEndDay(out _), Is.True);
                Assert.That(manager.CurrentEvent, Is.SameAs(major));

                Assert.That(manager.TryApplyChoice(manager.CurrentEvent, 0, out _), Is.True);

                Assert.That(time.Day, Is.GreaterThan(dayBeforeEnd));
            }
            finally
            {
                for (var i = 0; i < createdEvents.Count; i++)
                {
                    if (createdEvents[i] != null)
                    {
                        Object.DestroyImmediate(createdEvents[i]);
                    }
                }

                for (var i = 0; i < createdObjects.Count; i++)
                {
                    if (createdObjects[i] != null)
                    {
                        Object.DestroyImmediate(createdObjects[i]);
                    }
                }
            }
        }

        private static T TrackObject<T>(ICollection<Object> bag, T obj)
            where T : Object
        {
            bag.Add(obj);
            return obj;
        }

        private static EventData TrackEvent(ICollection<EventData> bag, EventData eventData)
        {
            bag.Add(eventData);
            return eventData;
        }

        private static EventData CreateEvent(string eventId, string category)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "title", eventId);
            SetField(eventData, "description", "playmode smoke");
            SetField(eventData, "category", category);
            SetField(eventData, "selectionWeight", 1f);
            SetField(eventData, "choices", new List<EventChoice> { new EventChoice() });
            return eventData;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
        }

        private static EndingDatabase CreatePopulatedEndingDatabase()
        {
            var database = ScriptableObject.CreateInstance<EndingDatabase>();
            var entries = new List<EndingTextEntry>();
            var ids = (EndingId[])System.Enum.GetValues(typeof(EndingId));
            for (var i = 0; i < ids.Length; i++)
            {
                if (ids[i] == EndingId.None)
                {
                    continue;
                }

                var entry = new EndingTextEntry();
                entry.SetRuntimeValues(ids[i], $"Title {ids[i]}", $"Body {ids[i]}");
                entries.Add(entry);
            }

            database.SetRuntimeEntries(entries.ToArray());
            return database;
        }
    }
}
