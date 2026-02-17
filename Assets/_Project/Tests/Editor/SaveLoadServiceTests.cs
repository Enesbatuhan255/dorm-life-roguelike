using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class SaveLoadServiceTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;
        private string tempSaveRoot;

        [SetUp]
        public void SetUp()
        {
            tempSaveRoot = Path.Combine("Temp", "SaveLoadServiceTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempSaveRoot);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(tempSaveRoot))
            {
                Directory.Delete(tempSaveRoot, recursive: true);
            }
        }

        [Test]
        public void SaveAndLoad_Roundtrip_RestoresTimeStatsAndFlags()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var flags = new FlagStateService();

            using var saveLoad = new SaveLoadService(time, stats, flags, tempSaveRoot);

            time.AdvanceTime(30);
            stats.SetBaseValue(StatType.Hunger, 62f);
            stats.SetBaseValue(StatType.Mental, 41f);
            stats.SetBaseValue(StatType.Energy, 35f);
            stats.SetBaseValue(StatType.Money, -275f);
            stats.SetBaseValue(StatType.Academic, 2.35f);
            flags.ReplaceAll(
                new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                {
                    ["debt_pressure"] = 3f
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["kyk_status"] = "Warning"
                });

            saveLoad.SaveToSlot(SaveLoadService.Slot1);

            time.AdvanceTime(17);
            stats.SetBaseValue(StatType.Hunger, 11f);
            stats.SetBaseValue(StatType.Mental, 12f);
            stats.SetBaseValue(StatType.Energy, 13f);
            stats.SetBaseValue(StatType.Money, 14f);
            stats.SetBaseValue(StatType.Academic, 0.5f);
            flags.ReplaceAll(
                new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase)
                {
                    ["debt_pressure"] = 99f,
                    ["temp_only"] = 1f
                },
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["kyk_status"] = "Cut",
                    ["temp_text"] = "X"
                });

            var loaded = saveLoad.LoadFromSlot(SaveLoadService.Slot1);

            Assert.That(loaded, Is.True);
            Assert.That(time.Day, Is.EqualTo(2));
            Assert.That(time.Hour, Is.EqualTo(14));
            Assert.That(stats.GetStat(StatType.Hunger), Is.EqualTo(62f).Within(0.001f));
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(41f).Within(0.001f));
            Assert.That(stats.GetStat(StatType.Energy), Is.EqualTo(35f).Within(0.001f));
            Assert.That(stats.GetStat(StatType.Money), Is.EqualTo(-275f).Within(0.001f));
            Assert.That(stats.GetStat(StatType.Academic), Is.EqualTo(2.35f).Within(0.001f));
            Assert.That(flags.TryGetNumeric("debt_pressure", out var debtPressure), Is.True);
            Assert.That(debtPressure, Is.EqualTo(3f).Within(0.001f));
            Assert.That(flags.TryGetNumeric("temp_only", out _), Is.False);
            Assert.That(flags.TryGetText("kyk_status", out var kykStatus), Is.True);
            Assert.That(kykStatus, Is.EqualTo("Warning"));
            Assert.That(flags.TryGetText("temp_text", out _), Is.False);
        }

        [Test]
        public void OnDayChanged_CreatesQuickAutosave_OncePerDay()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var flags = new FlagStateService();

            using var saveLoad = new SaveLoadService(time, stats, flags, tempSaveRoot);

            var quickPath = Path.Combine(tempSaveRoot, SaveLoadService.Quick + ".json");
            Assert.That(File.Exists(quickPath), Is.False);

            time.AdvanceTime(24);

            Assert.That(File.Exists(quickPath), Is.True);
            var firstWrite = File.GetLastWriteTimeUtc(quickPath);

            time.AdvanceTime(1);
            var secondWrite = File.GetLastWriteTimeUtc(quickPath);
            Assert.That(secondWrite, Is.EqualTo(firstWrite));

            var summaries = saveLoad.GetSlotSummaries();
            var quick = FindBySlot(summaries, SaveLoadService.Quick);
            Assert.That(quick, Is.Not.Null);
            Assert.That(quick.Exists, Is.True);
            Assert.That(quick.Day, Is.EqualTo(2));
            Assert.That(quick.Hour, Is.EqualTo(8));
        }

        [Test]
        public void SaveAndLoad_Roundtrip_RestoresSchedulerQueueAndOutcomeState()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            var flags = new FlagStateService();
            var manager = new EventManager(stats, time, flags);

            var root = CreateEvent("EVT_SAVELOAD_ROOT", "Major", new[] { "EVT_SAVELOAD_FOLLOWUP" }, 2);
            var followUp = CreateEvent("EVT_SAVELOAD_FOLLOWUP", "Major");
            var pending = CreateEvent("EVT_SAVELOAD_PENDING", "Major");

            var createdObjects = new List<UnityEngine.Object>();
            var cooldownConfig = EventCooldownConfig.CreateRuntimeDefault(0);
            createdObjects.Add(cooldownConfig);
            var outcomeConfig = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            createdObjects.Add(outcomeConfig);
            outcomeConfig.SetRuntimeValues(
                targetDaysValue: 120,
                minAcademicPassValue: 0f,
                useAcademicFailThresholdValue: false,
                minAcademicFailValue: 0f,
                minMentalValue: 0f,
                useEnergyThresholdValue: false,
                minEnergyValue: 0f,
                minMoneyValue: -99999f,
                failPriorityValue: GameOutcomeFailPriority.AcademicFirst,
                debtEnforcementThresholdValue: -10f,
                debtEnforcementGraceDaysValue: 1);
            var academicConfig = AcademicConfig.CreateRuntimeDefault();
            createdObjects.Add(academicConfig);
            var endingDatabase = CreatePopulatedEndingDatabase();
            createdObjects.Add(endingDatabase);

            try
            {
                using var scheduler = new EventScheduler(
                    time,
                    manager,
                    stats,
                    new[] { root, followUp, pending },
                    checkIntervalHours: 1,
                    cooldownConfig: cooldownConfig,
                    flagStateService: flags);
                using var outcome = new GameOutcomeSystem(
                    time,
                    stats,
                    outcomeConfig,
                    academicConfig,
                    endingDatabase,
                    flags);
                using var saveLoad = new SaveLoadService(
                    time,
                    stats,
                    flags,
                    tempSaveRoot,
                    eventManager: manager,
                    eventScheduler: scheduler,
                    gameOutcomeSystem: outcome);

                Assert.That(manager.EnqueueEvent(root), Is.True);
                Assert.That(manager.TryApplyChoice(manager.CurrentEvent, 0, out _), Is.True);
                Assert.That(manager.CurrentEvent, Is.Null);

                Assert.That(manager.EnqueueEvent(pending), Is.True);
                Assert.That(manager.CurrentEvent, Is.SameAs(pending));

                stats.SetBaseValue(StatType.Money, -100f);
                time.AdvanceTime(24);
                Assert.That(outcome.IsResolved, Is.True);

                saveLoad.SaveToSlot(SaveLoadService.Slot2);

                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
                time.SetAbsoluteTimeForLoad(1, 8);
                stats.SetBaseValue(StatType.Money, 500f);
                flags.ReplaceAll(
                    new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase),
                    new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));

                var loaded = saveLoad.LoadFromSlot(SaveLoadService.Slot2);

                Assert.That(loaded, Is.True);
                Assert.That(time.Day, Is.EqualTo(2));
                Assert.That(manager.CurrentEvent, Is.Not.Null);
                Assert.That(manager.CurrentEvent.EventId, Is.EqualTo("EVT_SAVELOAD_PENDING"));
                Assert.That(outcome.IsResolved, Is.True);
                Assert.That(outcome.CurrentResult.Status, Is.EqualTo(GameOutcomeStatus.Lose));

                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
                Assert.That(manager.CurrentEvent, Is.Null);

                time.AdvanceTime(24);
                Assert.That(manager.CurrentEvent, Is.SameAs(followUp));
            }
            finally
            {
                EventTestFactory.Destroy(root, followUp, pending);

                for (var i = 0; i < createdObjects.Count; i++)
                {
                    if (createdObjects[i] != null)
                    {
                        UnityEngine.Object.DestroyImmediate(createdObjects[i]);
                    }
                }
            }
        }

        private static SaveSlotSummary FindBySlot(IReadOnlyList<SaveSlotSummary> summaries, string slotId)
        {
            for (var i = 0; i < summaries.Count; i++)
            {
                var summary = summaries[i];
                if (summary != null && string.Equals(summary.SlotId, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    return summary;
                }
            }

            return null;
        }

        private static EventData CreateEvent(string eventId, string category, string[] choiceFollowUpEventIds = null, int choiceFollowUpDelayDays = 0)
        {
            var eventData = ScriptableObject.CreateInstance<EventData>();
            SetField(eventData, "eventId", eventId);
            SetField(eventData, "title", eventId);
            SetField(eventData, "description", "save-load-test");
            SetField(eventData, "category", category);
            SetField(eventData, "selectionWeight", 1f);
            SetField(eventData, "requiredContextTags", new List<EventContextTag>());
            SetField(eventData, "followUpEventIds", new List<string>());
            SetField(eventData, "followUpDelayDays", 0);

            var choice = new EventChoice();
            SetField(choice, "text", "Choice");
            SetField(choice, "followUpEventIds", new List<string>(choiceFollowUpEventIds ?? new string[0]));
            SetField(choice, "followUpDelayDays", choiceFollowUpDelayDays);
            SetField(eventData, "choices", new List<EventChoice> { choice });
            return eventData;
        }

        private static EndingDatabase CreatePopulatedEndingDatabase()
        {
            var database = ScriptableObject.CreateInstance<EndingDatabase>();
            var entries = new List<EndingTextEntry>();
            var ids = (EndingId[])Enum.GetValues(typeof(EndingId));
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

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
        }
    }
}
