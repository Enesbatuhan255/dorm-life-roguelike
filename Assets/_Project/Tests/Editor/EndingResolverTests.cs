using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EndingResolverTests
    {
        private GameOutcomeConfig config;
        private EndingDatabase endingDatabase;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameOutcomeConfig>();
            config.hideFlags = HideFlags.DontSave;
            endingDatabase = CreatePopulatedEndingDatabase();
            endingDatabase.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }

            if (endingDatabase != null)
            {
                Object.DestroyImmediate(endingDatabase);
            }
        }

        [Test]
        public void EarlyFailure_WithSevereDebt_ResolvesExpelledDebtSpiral()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: true,
                isAcademicPass: false,
                mental: 80f,
                energy: 50f,
                money: -1600f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.ExpelledDebtSpiral));
            Assert.That(result.EpilogTitle, Is.Not.Empty);
            Assert.That(result.EpilogBody, Is.Not.Empty);
        }

        [Test]
        public void EarlyFailure_WithLowMental_ResolvesExpelledBurnout()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: true,
                isAcademicPass: false,
                mental: 20f,
                energy: 50f,
                money: -300f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.ExpelledBurnout));
        }

        [Test]
        public void DebtEnforcementTriggered_ResolvesDebtEnforcementPrison()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: true,
                isAcademicPass: false,
                mental: 80f,
                energy: 50f,
                money: -2200f,
                config: config,
                endingDatabase: endingDatabase,
                isDebtEnforcementTriggered: true);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.DebtEnforcementPrison));
            Assert.That(result.EmploymentState, Is.EqualTo(EmploymentState.Unemployed));
        }

        [Test]
        public void Pass_WithHeavyDebt_ResolvesGraduatedUnemployedDebt()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 60f,
                energy: 50f,
                money: -1200f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.GraduatedUnemployedDebt));
            Assert.That(result.EmploymentState, Is.EqualTo(EmploymentState.Unemployed));
        }

        [Test]
        public void Pass_WithLightDebt_ResolvesGraduatedMinWageDebt()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 60f,
                energy: 50f,
                money: -300f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.GraduatedMinWageDebt));
        }

        [Test]
        public void Pass_WithLowMental_ResolvesGraduatedPrecariousStable()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 40f,
                energy: 60f,
                money: 100f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.GraduatedPrecariousStable));
        }

        [Test]
        public void Pass_WithHealthyStats_ResolvesGraduatedResilient()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 70f,
                energy: 50f,
                money: 100f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.GraduatedResilient));
        }

        [Test]
        public void Fail_WithHeavyDebt_ResolvesFailedDebtTrap()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: false,
                mental: 70f,
                energy: 50f,
                money: -1200f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.FailedDebtTrap));
        }

        [Test]
        public void Fail_WithoutHeavyDebt_ResolvesFailedExtendedYear()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: false,
                mental: 70f,
                energy: 50f,
                money: -100f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.FailedExtendedYear));
        }

        [Test]
        public void Boundary_HeavyDebtThreshold_IsNotUnemployedDebt()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 70f,
                energy: 50f,
                money: -1000f,
                config: config,
                endingDatabase: endingDatabase);

            Assert.That(result.EndingId, Is.Not.EqualTo(EndingId.GraduatedUnemployedDebt));
        }

        [Test]
        public void MissingEntry_UsesFallbackText()
        {
            var missingDatabase = ScriptableObject.CreateInstance<EndingDatabase>();
            missingDatabase.hideFlags = HideFlags.DontSave;
            missingDatabase.SetRuntimeEntries(System.Array.Empty<EndingTextEntry>());
            missingDatabase.SetRuntimeFallback("Fallback Ending", "Fallback Body");

            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 70f,
                energy: 50f,
                money: 100f,
                config: config,
                endingDatabase: missingDatabase);

            Assert.That(result.EpilogTitle, Is.EqualTo("Fallback Ending"));
            Assert.That(result.EpilogBody, Is.EqualTo("Fallback Body"));

            Object.DestroyImmediate(missingDatabase);
        }

        [Test]
        public void ForcedEndingId_WhenProvided_HasPriorityOverCalculatedEnding()
        {
            var result = EndingResolver.Resolve(
                isEarlyFailure: false,
                isAcademicPass: true,
                mental: 90f,
                energy: 90f,
                money: 800f,
                config: config,
                endingDatabase: endingDatabase,
                isDebtEnforcementTriggered: false,
                forcedEndingId: EndingId.ExpelledBurnout);

            Assert.That(result.EndingId, Is.EqualTo(EndingId.ExpelledBurnout));
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
