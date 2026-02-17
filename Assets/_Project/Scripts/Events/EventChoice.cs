using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class EventChoice
    {
        [SerializeField] private string text = "Choice";
        [SerializeField] private List<StatEffect> effects = new List<StatEffect>();
        [SerializeField] private EventCondition condition = new EventCondition();
        [SerializeField] private List<string> followUpEventIds = new List<string>();
        [Min(0)]
        [SerializeField] private int timeAdvanceHours;

        public string Text => text;

        public IReadOnlyList<StatEffect> Effects => effects;

        public EventCondition Condition => condition;

        public IReadOnlyList<string> FollowUpEventIds => followUpEventIds;

        public int TimeAdvanceHours => timeAdvanceHours;
    }
}
