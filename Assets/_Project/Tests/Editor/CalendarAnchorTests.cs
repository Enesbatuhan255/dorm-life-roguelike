using DormLifeRoguelike;
using NUnit.Framework;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class CalendarAnchorTests
    {
        [Test]
        public void TotalDays_IsSeventyTwo()
        {
            var time = new TimeManager();
            Assert.That(time.TotalDaysInAcademicYear, Is.EqualTo(72));
        }

        [Test]
        public void InflationShock_DayThirtySeven_Only()
        {
            var time = new TimeManager();
            Assert.That(time.IsInflationShockDay(36), Is.False);
            Assert.That(time.IsInflationShockDay(37), Is.True);
            Assert.That(time.IsInflationShockDay(38), Is.False);
        }

        [Test]
        public void ExamWindows_AreMappedCorrectly()
        {
            var time = new TimeManager();

            Assert.That(time.IsInExamWindow(16), Is.True);
            Assert.That(time.IsInExamWindow(19), Is.True);
            Assert.That(time.IsInExamWindow(34), Is.True);
            Assert.That(time.IsInExamWindow(36), Is.True);
            Assert.That(time.IsInExamWindow(52), Is.True);
            Assert.That(time.IsInExamWindow(55), Is.True);
            Assert.That(time.IsInExamWindow(70), Is.True);
            Assert.That(time.IsInExamWindow(72), Is.True);

            Assert.That(time.IsInExamWindow(20), Is.False);
            Assert.That(time.IsInExamWindow(37), Is.False);
            Assert.That(time.IsInExamWindow(69), Is.False);
        }

        [Test]
        public void KykPaydays_AreMappedCorrectly()
        {
            var time = new TimeManager();

            Assert.That(time.IsKykPayday(1), Is.True);
            Assert.That(time.IsKykPayday(15), Is.True);
            Assert.That(time.IsKykPayday(29), Is.True);
            Assert.That(time.IsKykPayday(43), Is.True);
            Assert.That(time.IsKykPayday(57), Is.True);
            Assert.That(time.IsKykPayday(71), Is.True);
            Assert.That(time.IsKykPayday(37), Is.False);
        }
    }
}
