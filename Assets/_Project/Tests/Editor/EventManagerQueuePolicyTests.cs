using System.Collections.Generic;
using DormLifeRoguelike;
using NUnit.Framework;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventManagerQueuePolicyTests
    {
        private readonly List<EventData> createdEvents = new List<EventData>();

        [TearDown]
        public void TearDown()
        {
            EventTestFactory.Destroy(createdEvents.ToArray());
            createdEvents.Clear();
        }

        [Test]
        public void Enqueue_DuplicateActiveEvent_IsSkipped()
        {
            var manager = new EventManager(new StatSystem(), new TimeManager());
            var outcomes = new List<string>();
            manager.OnOutcomeLogged += outcomes.Add;

            var eventData = Track(EventTestFactory.CreateEvent("EVT_TEST_DUPLICATE", true));

            manager.EnqueueEvent(eventData);
            manager.EnqueueEvent(eventData);

            Assert.That(manager.CurrentEvent, Is.SameAs(eventData));
            Assert.That(outcomes, Has.Some.Contains("Skipped duplicate enqueue for active event"));
        }

        [Test]
        public void Enqueue_BeyondMaxPendingQueueSize_IsDropped()
        {
            var manager = new EventManager(new StatSystem(), new TimeManager());
            var completedCount = 0;
            manager.OnEventCompleted += _ => completedCount++;

            for (var i = 0; i < 7; i++)
            {
                var eventData = Track(EventTestFactory.CreateEvent($"EVT_TEST_{i}", true));
                manager.EnqueueEvent(eventData);
            }

            while (manager.CurrentEvent != null)
            {
                manager.TryApplyChoice(manager.CurrentEvent, 0, out _);
            }

            Assert.That(completedCount, Is.EqualTo(6));
            Assert.That(manager.CurrentEvent, Is.Null);
            Assert.That(manager.HasPendingEvents, Is.False);
        }

        [Test]
        public void Enqueue_DifferentAssetWithSameActiveEventId_IsSkipped()
        {
            var manager = new EventManager(new StatSystem(), new TimeManager());
            var outcomes = new List<string>();
            manager.OnOutcomeLogged += outcomes.Add;

            var activeEvent = Track(EventTestFactory.CreateEvent("EVT_TEST_SHARED_ID", true));
            var duplicateById = Track(EventTestFactory.CreateEvent("EVT_TEST_SHARED_ID", true));

            manager.EnqueueEvent(activeEvent);
            manager.EnqueueEvent(duplicateById);

            Assert.That(manager.CurrentEvent, Is.SameAs(activeEvent));
            Assert.That(outcomes, Has.Some.Contains("Skipped duplicate enqueue for active event"));
        }

        private EventData Track(EventData eventData)
        {
            createdEvents.Add(eventData);
            return eventData;
        }
    }
}
