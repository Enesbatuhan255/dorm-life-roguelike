using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class EventChoice
    {
        [SerializeField] private string text = "Choice";
        [TextArea(2, 5)]
        [SerializeField] private string notes = string.Empty;
        [SerializeField] private List<StatEffect> effects = new List<StatEffect>();
        [SerializeField] private List<EventFlagChange> flags = new List<EventFlagChange>();
        [SerializeField] private EventCondition condition = new EventCondition();
        [SerializeField] private List<string> followUpEventIds = new List<string>();
        [Min(0)]
        [SerializeField] private int followUpDelayDays;
        [Min(0)]
        [SerializeField] private int timeAdvanceHours;

        public string Text => text;

        public string Notes => notes;

        public IReadOnlyList<StatEffect> Effects => effects;

        public IReadOnlyList<EventFlagChange> Flags => flags;

        public EventCondition Condition => condition;

        public IReadOnlyList<string> FollowUpEventIds => followUpEventIds;

        public int FollowUpDelayDays => followUpDelayDays;

        public int TimeAdvanceHours => timeAdvanceHours;
    }
}
