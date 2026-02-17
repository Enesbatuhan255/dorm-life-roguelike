namespace DormLifeRoguelike
{
    public sealed class StatChangedEventArgs
    {
        public StatChangedEventArgs(StatType statType, float oldValue, float newValue, float appliedDelta)
        {
            StatType = statType;
            OldValue = oldValue;
            NewValue = newValue;
            AppliedDelta = appliedDelta;
        }

        public StatType StatType { get; }

        public float OldValue { get; }

        public float NewValue { get; }

        public float AppliedDelta { get; }
    }
}
