using System.Collections.Generic;
using System.Reflection;
using DormLifeRoguelike;
using NUnit.Framework;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventFlagStateTests
    {
        private const BindingFlags InstancePrivate = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<EventData> createdEvents = new List<EventData>();

        [TearDown]
        public void TearDown()
        {
            EventTestFactory.Destroy(createdEvents.ToArray());
            createdEvents.Clear();
        }

        [Test]
        public void TryApplyChoice_AppliesChoiceFlags_ToFlagStateService()
        {
            var stats = new StatSystem();
            var time = new TimeManager();
            var flags = new FlagStateService();
            var manager = new EventManager(stats, time, flags);
            var evt = Track(EventTestFactory.CreateEvent("EVT_FLAGS_TEST", true));
            var choice = evt.Choices[0];

            SetField(choice, "flags", new List<EventFlagChange>
            {
                NewNumericFlag("debt_pressure", 2f),
                NewNumericFlag("debt_pressure", -1f),
                NewTextFlag("kyk_status", "Cut")
            });

            manager.EnqueueEvent(evt);
            var ok = manager.TryApplyChoice(evt, 0, out _);

            Assert.That(ok, Is.True);
            Assert.That(flags.TryGetNumeric("debt_pressure", out var debtPressure), Is.True);
            Assert.That(debtPressure, Is.EqualTo(1f));
            Assert.That(flags.TryGetText("kyk_status", out var kykStatus), Is.True);
            Assert.That(kykStatus, Is.EqualTo("Cut"));
        }

        private EventData Track(EventData eventData)
        {
            createdEvents.Add(eventData);
            return eventData;
        }

        private static EventFlagChange NewNumericFlag(string key, float value)
        {
            var flag = new EventFlagChange();
            SetField(flag, "key", key);
            SetField(flag, "mode", EventFlagChangeMode.AddNumeric);
            SetField(flag, "numericValue", value);
            SetField(flag, "textValue", string.Empty);
            return flag;
        }

        private static EventFlagChange NewTextFlag(string key, string value)
        {
            var flag = new EventFlagChange();
            SetField(flag, "key", key);
            SetField(flag, "mode", EventFlagChangeMode.SetText);
            SetField(flag, "numericValue", 0f);
            SetField(flag, "textValue", value);
            return flag;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, InstancePrivate);
            field.SetValue(target, value);
        }
    }
}
