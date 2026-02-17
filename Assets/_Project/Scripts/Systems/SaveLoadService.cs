using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class SaveLoadService : ISaveLoadService, IDisposable
    {
        public const string Slot1 = "slot_1";
        public const string Slot2 = "slot_2";
        public const string Slot3 = "slot_3";
        public const string Quick = "quick";

        private static readonly string[] KnownSlots = { Slot1, Slot2, Slot3, Quick };

        private readonly ITimeManager timeManager;
        private readonly IStatSystem statSystem;
        private readonly IFlagStateService flagStateService;
        private readonly IEventManager eventManager;
        private readonly IEventScheduler eventScheduler;
        private readonly IGameOutcomeSystem gameOutcomeSystem;
        private readonly ISnapshotMigrator migrator;
        private readonly string saveRootPath;
        private int lastAutosaveDay = -1;
        private bool isDisposed;

        public SaveLoadService(
            ITimeManager timeManager,
            IStatSystem statSystem,
            IFlagStateService flagStateService,
            string saveRootPath = null,
            ISnapshotMigrator migrator = null,
            IEventManager eventManager = null,
            IEventScheduler eventScheduler = null,
            IGameOutcomeSystem gameOutcomeSystem = null)
        {
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.flagStateService = flagStateService ?? throw new ArgumentNullException(nameof(flagStateService));
            this.eventManager = eventManager;
            this.eventScheduler = eventScheduler;
            this.gameOutcomeSystem = gameOutcomeSystem;
            this.migrator = migrator ?? new SnapshotMigrator();
            this.saveRootPath = string.IsNullOrWhiteSpace(saveRootPath)
                ? Path.Combine(Application.persistentDataPath, "DormLifeRoguelike", "saves")
                : saveRootPath;

            Directory.CreateDirectory(this.saveRootPath);
            this.timeManager.OnDayChanged += HandleDayChanged;
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
        }

        public void SaveToSlot(string slotId)
        {
            var normalized = NormalizeSlotId(slotId);
            var snapshot = CaptureSnapshot(normalized);
            var json = JsonUtility.ToJson(snapshot, true);
            File.WriteAllText(GetSlotPath(normalized), json);
        }

        public bool LoadFromSlot(string slotId)
        {
            var normalized = NormalizeSlotId(slotId);
            var path = GetSlotPath(normalized);
            if (!File.Exists(path))
            {
                return false;
            }

            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            var snapshot = JsonUtility.FromJson<GameSnapshot>(json);
            snapshot = migrator.Migrate(snapshot);
            if (snapshot == null)
            {
                return false;
            }

            RestoreSnapshot(snapshot);
            return true;
        }

        public void SaveQuick()
        {
            SaveToSlot(Quick);
        }

        public bool LoadQuick()
        {
            return LoadFromSlot(Quick);
        }

        public IReadOnlyList<SaveSlotSummary> GetSlotSummaries()
        {
            var list = new List<SaveSlotSummary>(KnownSlots.Length);
            for (var i = 0; i < KnownSlots.Length; i++)
            {
                var slot = KnownSlots[i];
                var path = GetSlotPath(slot);
                if (!File.Exists(path))
                {
                    list.Add(new SaveSlotSummary(slot, false, string.Empty, 0, 0));
                    continue;
                }

                var json = File.ReadAllText(path);
                var snapshot = JsonUtility.FromJson<GameSnapshot>(json);
                snapshot = migrator.Migrate(snapshot);
                if (snapshot == null)
                {
                    list.Add(new SaveSlotSummary(slot, true, "INVALID", 0, 0));
                    continue;
                }

                list.Add(new SaveSlotSummary(
                    slot,
                    true,
                    snapshot.savedAtUtc,
                    snapshot.time != null ? snapshot.time.day : 0,
                    snapshot.time != null ? snapshot.time.hour : 0));
            }

            return list;
        }

        private void HandleDayChanged(int day)
        {
            if (day <= 0 || day == lastAutosaveDay)
            {
                return;
            }

            lastAutosaveDay = day;
            SaveQuick();
        }

        private GameSnapshot CaptureSnapshot(string slotId)
        {
            var numericFlags = flagStateService.ExportNumericSnapshot();
            var textFlags = flagStateService.ExportTextSnapshot();

            var snapshot = new GameSnapshot
            {
                schemaVersion = SnapshotMigrator.CurrentSchemaVersion,
                savedAtUtc = DateTime.UtcNow.ToString("O"),
                slotId = slotId,
                time = new TimeSnapshot
                {
                    day = timeManager.Day,
                    hour = timeManager.Hour,
                    weekIndex = timeManager.WeekIndex,
                    monthIndex = timeManager.MonthIndex
                },
                stats = new StatSnapshot
                {
                    hunger = statSystem.GetStat(StatType.Hunger),
                    mental = statSystem.GetStat(StatType.Mental),
                    energy = statSystem.GetStat(StatType.Energy),
                    money = statSystem.GetStat(StatType.Money),
                    academic = statSystem.GetStat(StatType.Academic)
                },
                flags = new FlagSnapshot()
            };

            foreach (var pair in numericFlags)
            {
                snapshot.flags.numeric.Add(new NumericFlagEntry { key = pair.Key, value = pair.Value });
            }

            foreach (var pair in textFlags)
            {
                snapshot.flags.text.Add(new TextFlagEntry { key = pair.Key, value = pair.Value });
            }

            if (eventManager is EventManager concreteEventManager)
            {
                snapshot.eventManager = concreteEventManager.CaptureRuntimeSnapshot();
            }

            if (eventScheduler is EventScheduler concreteScheduler)
            {
                snapshot.eventScheduler = concreteScheduler.CaptureRuntimeSnapshot();
            }

            if (gameOutcomeSystem is GameOutcomeSystem concreteOutcomeSystem)
            {
                snapshot.gameOutcome = concreteOutcomeSystem.CaptureRuntimeSnapshot();
            }

            return snapshot;
        }

        private void RestoreSnapshot(GameSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            var time = snapshot.time ?? new TimeSnapshot();
            timeManager.SetAbsoluteTimeForLoad(time.day, time.hour);

            var stats = snapshot.stats ?? new StatSnapshot();
            statSystem.SetBaseValue(StatType.Hunger, stats.hunger);
            statSystem.SetBaseValue(StatType.Mental, stats.mental);
            statSystem.SetBaseValue(StatType.Energy, stats.energy);
            statSystem.SetBaseValue(StatType.Money, stats.money);
            statSystem.SetBaseValue(StatType.Academic, stats.academic);

            var numeric = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
            var text = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var flags = snapshot.flags ?? new FlagSnapshot();

            if (flags.numeric != null)
            {
                for (var i = 0; i < flags.numeric.Count; i++)
                {
                    var entry = flags.numeric[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    numeric[entry.key.Trim()] = entry.value;
                }
            }

            if (flags.text != null)
            {
                for (var i = 0; i < flags.text.Count; i++)
                {
                    var entry = flags.text[i];
                    if (entry == null || string.IsNullOrWhiteSpace(entry.key))
                    {
                        continue;
                    }

                    text[entry.key.Trim()] = entry.value ?? string.Empty;
                }
            }

            flagStateService.ReplaceAll(numeric, text);

            IReadOnlyDictionary<string, EventData> eventLookup = null;
            if (eventScheduler is EventScheduler concreteScheduler)
            {
                concreteScheduler.RestoreRuntimeSnapshot(snapshot.eventScheduler);
                eventLookup = concreteScheduler.ExportEventLookup();
            }

            if (eventManager is EventManager concreteEventManager)
            {
                concreteEventManager.RestoreRuntimeSnapshot(snapshot.eventManager, eventLookup);
            }

            if (gameOutcomeSystem is GameOutcomeSystem concreteOutcomeSystem)
            {
                concreteOutcomeSystem.RestoreRuntimeSnapshot(snapshot.gameOutcome);
            }

            lastAutosaveDay = timeManager.Day;
        }

        private string GetSlotPath(string slotId)
        {
            return Path.Combine(saveRootPath, slotId + ".json");
        }

        private static string NormalizeSlotId(string slotId)
        {
            if (string.IsNullOrWhiteSpace(slotId))
            {
                throw new ArgumentException("slotId is required.", nameof(slotId));
            }

            return slotId.Trim().ToLowerInvariant();
        }
    }
}
