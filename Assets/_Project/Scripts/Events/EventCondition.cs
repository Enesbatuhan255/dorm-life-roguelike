using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class EventCondition
    {
        [SerializeField] private bool isEnabled;
        [SerializeField] private StatType statType = StatType.Hunger;
        [SerializeField] private ConditionOperator comparisonOperator = ConditionOperator.GreaterThanOrEqual;
        [SerializeField] private float value;

        public bool IsEnabled => isEnabled;

        public StatType StatType => statType;

        public ConditionOperator Operator => comparisonOperator;

        public float Value => value;
    }
}
