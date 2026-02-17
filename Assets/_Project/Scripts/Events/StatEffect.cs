using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class StatEffect
    {
        [SerializeField] private StatType statType = StatType.Hunger;
        [SerializeField] private float delta;

        public StatType StatType => statType;

        public float Delta => delta;
    }
}
