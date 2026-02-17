using System;

namespace DormLifeRoguelike
{
    public interface IStatSystem : IService
    {
        event Action<StatChangedEventArgs> OnStatChanged;

        float GetStat(StatType statType);

        void ApplyBaseDelta(StatType statType, float delta);

        void SetBaseValue(StatType statType, float value);

        void SetModifier(string sourceId, StatType statType, float modifierDelta);

        void RemoveModifier(string sourceId, StatType statType);
    }
}
