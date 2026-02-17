using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class InflationShockTests
    {
        private InflationShockConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<InflationShockConfig>();
            config.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Multiplier_AppliesOnlyAfterTriggerDay()
        {
            var time = new TimeManager();
            using var system = new InflationShockSystem(time, config);

            Assert.That(system.ApplyToCost(-10f), Is.EqualTo(-10f).Within(0.001f));
            time.AdvanceTime(24 * 37);
            Assert.That(system.IsTriggered, Is.True);
            Assert.That(system.ApplyToCost(-10f), Is.EqualTo(-12f).Within(0.001f));
        }
    }
}
