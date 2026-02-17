using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public interface ISaveLoadService : IService
    {
        void SaveToSlot(string slotId);
        bool LoadFromSlot(string slotId);
        void SaveQuick();
        bool LoadQuick();
        IReadOnlyList<SaveSlotSummary> GetSlotSummaries();
    }
}
