using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class StatSystem : IStatSystem
    {
        private const float MinBoundDefault = 0f;
        private const float MaxBoundDefault = 100f;

        private readonly Dictionary<StatType, float> baseValues;
        private readonly Dictionary<StatType, Dictionary<string, float>> modifiers;

        public StatSystem()
        {
            baseValues = new Dictionary<StatType, float>
            {
                { StatType.Hunger, 100f },
                { StatType.Mental, 100f },
                { StatType.Energy, 100f },
                { StatType.Money, 0f },
                { StatType.Academic, 0f }
            };

            modifiers = new Dictionary<StatType, Dictionary<string, float>>
            {
                { StatType.Hunger, new Dictionary<string, float>() },
                { StatType.Mental, new Dictionary<string, float>() },
                { StatType.Energy, new Dictionary<string, float>() },
                { StatType.Money, new Dictionary<string, float>() },
                { StatType.Academic, new Dictionary<string, float>() }
            };
        }

        public event Action<StatChangedEventArgs> OnStatChanged;

        public float GetStat(StatType statType)
        {
            return CalculateEffectiveValue(statType);
        }

        public void ApplyBaseDelta(StatType statType, float delta)
        {
            var oldValue = CalculateEffectiveValue(statType);
            baseValues[statType] += delta;
            var newValue = CalculateEffectiveValue(statType);
            RaiseIfChanged(statType, oldValue, newValue);
        }

        public void SetBaseValue(StatType statType, float value)
        {
            var oldValue = CalculateEffectiveValue(statType);
            baseValues[statType] = value;
            var newValue = CalculateEffectiveValue(statType);
            RaiseIfChanged(statType, oldValue, newValue);
        }

        public void SetModifier(string sourceId, StatType statType, float modifierDelta)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                throw new ArgumentException("Modifier sourceId cannot be null or empty.", nameof(sourceId));
            }

            var oldValue = CalculateEffectiveValue(statType);
            modifiers[statType][sourceId] = modifierDelta;
            var newValue = CalculateEffectiveValue(statType);
            RaiseIfChanged(statType, oldValue, newValue);
        }

        public void RemoveModifier(string sourceId, StatType statType)
        {
            if (string.IsNullOrWhiteSpace(sourceId))
            {
                return;
            }

            if (!modifiers[statType].ContainsKey(sourceId))
            {
                return;
            }

            var oldValue = CalculateEffectiveValue(statType);
            modifiers[statType].Remove(sourceId);
            var newValue = CalculateEffectiveValue(statType);
            RaiseIfChanged(statType, oldValue, newValue);
        }

        private float CalculateEffectiveValue(StatType statType)
        {
            var value = baseValues[statType];

            foreach (var modifier in modifiers[statType].Values)
            {
                value += modifier;
            }

            if (statType == StatType.Money)
            {
                return value;
            }

            if (statType == StatType.Energy)
            {
                return Mathf.Clamp(value, -10f, MaxBoundDefault);
            }

            if (statType == StatType.Academic)
            {
                return Mathf.Clamp(value, MinBoundDefault, 4f);
            }

            return Mathf.Clamp(value, MinBoundDefault, MaxBoundDefault);
        }

        private void RaiseIfChanged(StatType statType, float oldValue, float newValue)
        {
            if (Mathf.Approximately(oldValue, newValue))
            {
                return;
            }

            OnStatChanged?.Invoke(new StatChangedEventArgs(
                statType,
                oldValue,
                newValue,
                newValue - oldValue));
        }
    }
}
