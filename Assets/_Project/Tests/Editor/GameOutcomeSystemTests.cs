using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class GameOutcomeSystemTests
    {
        private GameOutcomeConfig outcomeConfig;
        private AcademicConfig academicConfig;
        private EndingDatabase endingDatabase;

        [SetUp]
        public void SetUp()
        {
            outcomeConfig = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            outcomeConfig.hideFlags = HideFlags.DontSave;
            outcomeConfig.SetRuntimeValues(
                targetDaysValue: 72,
                minAcademicPassValue: 0f,
                useAcademicFailThresholdValue: false,
                minAcademicFailValue: 0f,
                minMentalValue: 0f,
                useEnergyThresholdValue: false,
                minEnergyValue: 0f,
                minMoneyValue: -99999f,
                failPriorityValue: GameOutcomeFailPriority.AcademicFirst);

            academicConfig = ScriptableObject.CreateInstance<AcademicConfig>();
            academicConfig.hideFlags = HideFlags.DontSave;

            endingDatabase = CreatePopulatedEndingDatabase();
            endingDatabase.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            if (outcomeConfig != null)
            {
                Object.DestroyImmediate(outcomeConfig);
            }

            if (academicConfig != null)
            {
                Object.DestroyImmediate(academicConfig);
            }

            if (endingDatabase != null)
            {
                Object.DestroyImmediate(endingDatabase);
            }
        }

        [Test]
        public void AcademicBelowCritical_ForGraceWindow_ResolvesFailWithBand()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 1.7f);
            stats.SetBaseValue(StatType.Mental, 10f);

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            time.AdvanceTime(24 * 7);

            Assert.That(outcome.IsResolved, Is.True);
            Assert.That(outcome.CurrentResult.Status, Is.EqualTo(GameOutcomeStatus.Lose));
            Assert.That(outcome.CurrentResult.ScoreBand, Is.Not.EqualTo("Unrated"));
            Assert.That(outcome.CurrentResult.Score, Is.LessThanOrEqualTo(64));
            Assert.That(outcome.CurrentResult.EndingId, Is.EqualTo(EndingId.ExpelledBurnout));
        }

        [Test]
        public void AcademicRecoveredBeforeGraceWindow_DoesNotResolve()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 1.7f);

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            time.AdvanceTime(24 * 5);
            stats.SetBaseValue(StatType.Academic, 2.1f);
            time.AdvanceTime(24 * 3);

            Assert.That(outcome.IsResolved, Is.False);
        }

        [Test]
        public void EndOfCampaign_ResolvesWithScoreBand()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 3.5f);
            stats.SetBaseValue(StatType.Mental, 80f);
            stats.SetBaseValue(StatType.Energy, 70f);
            stats.SetBaseValue(StatType.Money, 600f);

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            time.AdvanceTime(24 * 72);

            Assert.That(outcome.IsResolved, Is.True);
            Assert.That(outcome.CurrentResult.ScoreBand, Is.EqualTo("Strong Recovery"));
            Assert.That(outcome.CurrentResult.Status, Is.EqualTo(GameOutcomeStatus.Win));
            Assert.That(outcome.CurrentResult.EndingId, Is.EqualTo(EndingId.GraduatedResilient));
        }

        [Test]
        public void DebtTooHigh_ForGraceDays_ResolvesDebtEnforcementPrison()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 3.2f);
            stats.SetBaseValue(StatType.Mental, 70f);
            stats.SetBaseValue(StatType.Energy, 60f);
            stats.SetBaseValue(StatType.Money, -2200f);

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            time.AdvanceTime(24 * 3);

            Assert.That(outcome.IsResolved, Is.True);
            Assert.That(outcome.CurrentResult.Status, Is.EqualTo(GameOutcomeStatus.Lose));
            Assert.That(outcome.CurrentResult.EndingId, Is.EqualTo(EndingId.DebtEnforcementPrison));
        }

        [Test]
        public void DebtRecovered_BeforeGraceWindow_DoesNotResolve()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 3.2f);
            stats.SetBaseValue(StatType.Mental, 70f);
            stats.SetBaseValue(StatType.Energy, 60f);
            stats.SetBaseValue(StatType.Money, -2200f);

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase);
            time.AdvanceTime(24 * 2);
            stats.SetBaseValue(StatType.Money, -500f);
            time.AdvanceTime(24 * 3);

            Assert.That(outcome.IsResolved, Is.False);
        }

        [Test]
        public void ForcedEndingFlag_WhenValid_OverridesDefaultResolverEnding()
        {
            var time = new TimeManager();
            var stats = new StatSystem();
            stats.SetBaseValue(StatType.Academic, 3.8f);
            stats.SetBaseValue(StatType.Mental, 85f);
            stats.SetBaseValue(StatType.Energy, 80f);
            stats.SetBaseValue(StatType.Money, 900f);

            var flags = new FlagStateService();
            flags.ReplaceAll(
                new Dictionary<string, float>(),
                new Dictionary<string, string>
                {
                    { "forced_ending_id", "FailedDebtTrap" }
                });

            using var outcome = new GameOutcomeSystem(time, stats, outcomeConfig, academicConfig, endingDatabase, flags);
            time.AdvanceTime(24 * 72);

            Assert.That(outcome.IsResolved, Is.True);
            Assert.That(outcome.CurrentResult.EndingId, Is.EqualTo(EndingId.FailedDebtTrap));
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
            database.SetRuntimeFallback("Fallback", "Fallback Body");
            return database;
        }
    }
}
