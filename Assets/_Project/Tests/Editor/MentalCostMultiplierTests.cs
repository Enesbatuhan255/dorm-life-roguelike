using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class MentalCostMultiplierTests
    {
        private MentalConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<MentalConfig>();
            config.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void StudyEnergyMultiplier_UsesConfiguredBands()
        {
            Assert.That(StudyCostCalculator.CalculateEnergyDelta(-12f, 80f, config), Is.EqualTo(-8.4f).Within(0.001f));
            Assert.That(StudyCostCalculator.CalculateEnergyDelta(-12f, 50f, config), Is.EqualTo(-12f).Within(0.001f));
            Assert.That(StudyCostCalculator.CalculateEnergyDelta(-12f, 20f, config), Is.EqualTo(-18f).Within(0.001f));
        }
    }
}
