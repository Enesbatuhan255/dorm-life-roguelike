using DormLifeRoguelike;
using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class WorkLimitTests
    {
        private WorkLimitConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<WorkLimitConfig>();
            config.hideFlags = HideFlags.DontSave;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        [Test]
        public void MaxThreeWorkActions_PerWeek()
        {
            var time = new TimeManager();
            using var work = new WorkLimitSystem(time, config);

            Assert.That(work.TryConsumeWorkAction(), Is.True);
            Assert.That(work.TryConsumeWorkAction(), Is.True);
            Assert.That(work.TryConsumeWorkAction(), Is.True);
            Assert.That(work.TryConsumeWorkAction(), Is.False);

            time.AdvanceTime(24 * 7);
            Assert.That(work.TryConsumeWorkAction(), Is.True);
        }
    }
}
