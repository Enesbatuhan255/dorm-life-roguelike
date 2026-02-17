using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EnergyOverdriveTests
    {
        private GameOutcomeConfig gameOutcomeConfig;
        private AcademicConfig academicConfig;
        private MentalConfig mentalConfig;
        private EndingDatabase endingDatabase;

        [SetUp]
        public void SetUp()
        {
            gameOutcomeConfig = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            gameOutcomeConfig.SetRuntimeValues(120, 0f, false, 0f, 0f, false, 0f, -99999f, GameOutcomeFailPriority.AcademicFirst);
            academicConfig = ScriptableObject.CreateInstance<AcademicConfig>();
            mentalConfig = ScriptableObject.CreateInstance<MentalConfig>();
            endingDatabase = CreatePopulatedEndingDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(gameOutcomeConfig);
            Object.DestroyImmediate(academicConfig);
            Object.DestroyImmediate(mentalConfig);
            Object.DestroyImmediate(endingDatabase);
        }

        [Test]
        public void SevereOverdrive_AppliesMentalAndNextDayEnergyPenalty()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Energy, 2f);
            var sleepDebtConfig = SleepDebtConfig.CreateRuntimeDefault();
            using var sleepDebt = new SleepDebtSystem(time, stats, sleepDebtConfig);
            using var economy = new EconomySystem(stats, time);
            using var outcome = new GameOutcomeSystem(time, stats, gameOutcomeConfig, academicConfig, endingDatabase);
            using var actions = new PlayerActionService(stats, time, sleepDebt, economy, outcome, null, null, null, mentalConfig, null);

            var mentalBefore = stats.GetStat(StatType.Mental);
            actions.ApplyWork(4);
            Assert.That(stats.GetStat(StatType.Energy), Is.EqualTo(-10f).Within(0.001f));

            actions.TryEndDay(out _);

            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(mentalBefore - 9f).Within(0.5f));
            Assert.That(stats.GetStat(StatType.Energy), Is.LessThanOrEqualTo(-10f));

            Object.DestroyImmediate(sleepDebtConfig);
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
