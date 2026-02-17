using System.Collections.Generic;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class DailyLoopOrderingTests
    {
        private readonly List<EventData> createdEvents = new List<EventData>();

        [TearDown]
        public void TearDown()
        {
            EventTestFactory.Destroy(createdEvents.ToArray());
            createdEvents.Clear();
        }

        [Test]
        public void MinorThenActionThenMajorThenDayAdvance()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var sleepDebtConfig = SleepDebtConfig.CreateRuntimeDefault();
            var mentalConfig = ScriptableObject.CreateInstance<MentalConfig>();
            var outcomeConfig = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            outcomeConfig.SetRuntimeValues(120, 0f, false, 0f, 0f, false, 0f, -99999f, GameOutcomeFailPriority.AcademicFirst);
            var academicConfig = ScriptableObject.CreateInstance<AcademicConfig>();
            var endingDatabase = CreatePopulatedEndingDatabase();

            var manager = new EventManager(stats, time);
            using var economy = new EconomySystem(stats, time);
            using var sleepDebt = new SleepDebtSystem(time, stats, sleepDebtConfig);
            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            var minor = Track(EventTestFactory.CreateEvent("EVT_LOOP_MINOR", true, category: "Minor"));
            var major = Track(EventTestFactory.CreateEvent("EVT_LOOP_MAJOR", true, category: "Major"));
            using var scheduler = new EventScheduler(time, manager, new[] { minor, major }, 1, 0);
            using var actions = new PlayerActionService(stats, time, sleepDebt, economy, outcome, scheduler, manager, null, mentalConfig, null);

            Assert.That(manager.CurrentEvent, Is.SameAs(minor));
            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);

            actions.ApplyWait(1);
            var dayBeforeEnd = time.Day;

            Assert.That(actions.TryEndDay(out _), Is.True);
            Assert.That(manager.CurrentEvent, Is.SameAs(major));

            manager.TryApplyChoice(manager.CurrentEvent, 0, out _);

            Assert.That(time.Day, Is.GreaterThan(dayBeforeEnd));

            Object.DestroyImmediate(mentalConfig);
            Object.DestroyImmediate(outcomeConfig);
            Object.DestroyImmediate(academicConfig);
            Object.DestroyImmediate(endingDatabase);
            Object.DestroyImmediate(sleepDebtConfig);
        }

        private EventData Track(EventData eventData)
        {
            createdEvents.Add(eventData);
            return eventData;
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
