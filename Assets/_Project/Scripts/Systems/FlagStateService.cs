using System;
using System.Collections.Generic;

namespace DormLifeRoguelike
{
    public sealed class FlagStateService : IFlagStateService
    {
        private readonly Dictionary<string, float> numericFlags = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> textFlags = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public void ApplyChanges(IReadOnlyList<EventFlagChange> changes)
        {
            if (changes == null)
            {
                return;
            }

            for (var i = 0; i < changes.Count; i++)
            {
                var change = changes[i];
                if (change == null || string.IsNullOrWhiteSpace(change.Key))
                {
                    continue;
                }

                var key = change.Key.Trim();
                switch (change.Mode)
                {
                    case EventFlagChangeMode.AddNumeric:
                        numericFlags.TryGetValue(key, out var current);
                        numericFlags[key] = current + change.NumericValue;
                        break;
                    case EventFlagChangeMode.SetText:
                        textFlags[key] = change.TextValue ?? string.Empty;
                        break;
                }
            }
        }

        public bool TryGetNumeric(string key, out float value)
        {
            value = 0f;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return numericFlags.TryGetValue(key.Trim(), out value);
        }

        public bool TryGetText(string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrWhiteSpace(key))
            {
                return false;
            }

            return textFlags.TryGetValue(key.Trim(), out value);
        }

        public Dictionary<string, float> ExportNumericSnapshot()
        {
            return new Dictionary<string, float>(numericFlags, StringComparer.OrdinalIgnoreCase);
        }

        public Dictionary<string, string> ExportTextSnapshot()
        {
            return new Dictionary<string, string>(textFlags, StringComparer.OrdinalIgnoreCase);
        }

        public void ReplaceAll(Dictionary<string, float> numeric, Dictionary<string, string> text)
        {
            numericFlags.Clear();
            textFlags.Clear();

            if (numeric != null)
            {
                foreach (var pair in numeric)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        numericFlags[pair.Key.Trim()] = pair.Value;
                    }
                }
            }

            if (text != null)
            {
                foreach (var pair in text)
                {
                    if (!string.IsNullOrWhiteSpace(pair.Key))
                    {
                        textFlags[pair.Key.Trim()] = pair.Value ?? string.Empty;
                    }
                }
            }
        }
    }
}
