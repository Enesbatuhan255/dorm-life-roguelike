using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public sealed class PlanSimulationResult
    {
        public PlanSimulationResult(int executedBlocks, int rejectedBlocks, IReadOnlyList<string> notes)
        {
            ExecutedBlocks = executedBlocks;
            RejectedBlocks = rejectedBlocks;
            Notes = notes;
        }

        public int ExecutedBlocks { get; }

        public int RejectedBlocks { get; }

        public IReadOnlyList<string> Notes { get; }
    }
}
