using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public sealed class DayPlan
    {
        public const int MaxBlocksPerDay = 8;
        public const int HoursPerBlock = 2;

        private readonly List<PlannedActionType> blocks = new List<PlannedActionType>(MaxBlocksPerDay);

        public IReadOnlyList<PlannedActionType> Blocks => blocks;

        public int BlockCount => blocks.Count;

        public bool IsFull => blocks.Count >= MaxBlocksPerDay;

        public bool TryAddBlock(PlannedActionType actionType)
        {
            if (IsFull)
            {
                return false;
            }

            blocks.Add(actionType);
            return true;
        }

        public void Clear()
        {
            blocks.Clear();
        }
    }
}
