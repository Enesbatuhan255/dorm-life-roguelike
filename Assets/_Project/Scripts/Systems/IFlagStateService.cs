using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public interface IFlagStateService : IService
    {
        void ApplyChanges(IReadOnlyList<EventFlagChange> changes);

        bool TryGetNumeric(string key, out float value);

        bool TryGetText(string key, out string value);

        Dictionary<string, float> ExportNumericSnapshot();

        Dictionary<string, string> ExportTextSnapshot();

        void ReplaceAll(Dictionary<string, float> numeric, Dictionary<string, string> text);
    }
}
