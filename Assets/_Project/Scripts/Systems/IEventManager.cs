using System;
using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public interface IEventManager : IService
    {
        event Action<EventData> OnEventStarted;
        event Action<EventData> OnEventCompleted;
        event Action<EventData, EventChoice> OnChoiceApplied;
        event Action<string> OnOutcomeLogged;

        EventData CurrentEvent { get; }

        bool HasPendingEvents { get; }

        void EnqueueEvent(EventData eventData);
        void PublishSystemMessage(string message);

        IReadOnlyList<EventChoice> GetAvailableChoices(EventData eventData);

        bool TryApplyChoice(EventData eventData, int choiceIndex, out string outcomeMessage);
    }
}
