using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class ActionServiceQualityOverloadTests
    {
        private GameOutcomeConfig outcomeConfig;
        private AcademicConfig academicConfig;
        private MentalConfig mentalConfig;
        private SleepDebtConfig sleepDebtConfig;
        private EndingDatabase endingDatabase;

        [SetUp]
        public void SetUp()
        {
            outcomeConfig = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            outcomeConfig.SetRuntimeValues(120, 0f, false, 0f, 0f, false, 0f, -99999f, GameOutcomeFailPriority.AcademicFirst);
            academicConfig = ScriptableObject.CreateInstance<AcademicConfig>();
            mentalConfig = ScriptableObject.CreateInstance<MentalConfig>();
            sleepDebtConfig = SleepDebtConfig.CreateRuntimeDefault();
            endingDatabase = CreatePopulatedEndingDatabase();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(outcomeConfig);
            Object.DestroyImmediate(academicConfig);
            Object.DestroyImmediate(mentalConfig);
            Object.DestroyImmediate(sleepDebtConfig);
            Object.DestroyImmediate(endingDatabase);
        }

        [Test]
        public void ApplyStudy_UsesOutcomeBandDeltas()
        {
            var (stats, _, _, _, actions) = CreateHarness();
            var academicBefore = stats.GetStat(StatType.Academic);
            var mentalBefore = stats.GetStat(StatType.Mental);

            actions.ApplyStudy(2, MicroChallengeOutcomeBand.Poor);

            Assert.That(stats.GetStat(StatType.Academic), Is.EqualTo(academicBefore + 0.15f).Within(0.02f));
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(mentalBefore - 6f).Within(0.05f));
        }

        [Test]
        public void ApplyWork_UsesOutcomeBandDeltas()
        {
            var (stats, _, _, _, actions) = CreateHarness();
            stats.SetBaseValue(StatType.Money, 0f);
            var mentalBefore = stats.GetStat(StatType.Mental);

            actions.ApplyWork(2, MicroChallengeOutcomeBand.Perfect);
            var moneyAfterPerfect = stats.GetStat(StatType.Money);

            actions.ApplyWork(2, MicroChallengeOutcomeBand.Poor);
            var moneyAfterPoor = stats.GetStat(StatType.Money);

            Assert.That(moneyAfterPerfect, Is.EqualTo(95f).Within(0.01f));
            Assert.That(moneyAfterPoor, Is.EqualTo(165f).Within(0.01f));
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(mentalBefore - 9f).Within(0.2f));
        }

        [Test]
        public void ApplyAdmin_UsesOutcomeBandDeltas()
        {
            var (stats, _, _, _, actions) = CreateHarness();
            stats.SetBaseValue(StatType.Money, 0f);
            var mentalBefore = stats.GetStat(StatType.Mental);

            actions.ApplyAdmin(2, MicroChallengeOutcomeBand.Perfect);
            actions.ApplyAdmin(2, MicroChallengeOutcomeBand.Poor);

            Assert.That(stats.GetStat(StatType.Money), Is.EqualTo(-5f).Within(0.01f));
            Assert.That(stats.GetStat(StatType.Mental), Is.EqualTo(mentalBefore - 1f).Within(0.01f));
        }

        private (StatSystem stats, TimeManager time, SleepDebtSystem sleepDebt, EconomySystem economy, PlayerActionService actions) CreateHarness()
        {
            var stats = new StatSystem();
            var time = new TimeManager();
            var sleepDebt = new SleepDebtSystem(time, stats, sleepDebtConfig);
            var economy = new EconomySystem(stats, time);
            var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            var actions = new PlayerActionService(stats, time, sleepDebt, economy, outcome, null, null, null, mentalConfig, null);
            return (stats, time, sleepDebt, economy, actions);
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
