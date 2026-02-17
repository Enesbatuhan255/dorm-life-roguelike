using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    public enum EventFlagChangeMode
    {
        AddNumeric = 0,
        SetText = 1
    }

    [Serializable]
    public sealed class EventFlagChange
    {
        [SerializeField] private string key = string.Empty;
        [SerializeField] private EventFlagChangeMode mode = EventFlagChangeMode.AddNumeric;
        [SerializeField] private float numericValue;
        [SerializeField] private string textValue = string.Empty;

        public string Key => key;
        public EventFlagChangeMode Mode => mode;
        public float NumericValue => numericValue;
        public string TextValue => textValue;
    }
}
