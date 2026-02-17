using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class EndingTextEntry
    {
        [SerializeField] private EndingId endingId = EndingId.None;
        [SerializeField] private string epilogTitle = string.Empty;
        [TextArea]
        [SerializeField] private string epilogBody = string.Empty;

        public EndingId EndingId => endingId;

        public string EpilogTitle => epilogTitle ?? string.Empty;

        public string EpilogBody => epilogBody ?? string.Empty;

        public void SetRuntimeValues(EndingId endingIdValue, string epilogTitleValue, string epilogBodyValue)
        {
            endingId = endingIdValue;
            epilogTitle = epilogTitleValue ?? string.Empty;
            epilogBody = epilogBodyValue ?? string.Empty;
        }
    }

    [CreateAssetMenu(fileName = "EndingDatabase", menuName = "DormLifeRoguelike/Config/EndingDatabase")]
    public sealed class EndingDatabase : ScriptableObject
    {
        [SerializeField] private List<EndingTextEntry> entries = new List<EndingTextEntry>();
        [SerializeField] private string fallbackTitle = "Hayat Devam Ediyor";
        [TextArea]
        [SerializeField] private string fallbackBody = "Sartlar agir. Plan bozuldu ama hikaye bitmedi.";

        public string FallbackTitle => fallbackTitle ?? string.Empty;

        public string FallbackBody => fallbackBody ?? string.Empty;

        public bool TryGetEntry(EndingId endingId, out EndingTextEntry entry)
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var candidate = entries[i];
                if (candidate != null && candidate.EndingId == endingId)
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = null;
            return false;
        }

        public void SetRuntimeFallback(string title, string body)
        {
            fallbackTitle = title ?? string.Empty;
            fallbackBody = body ?? string.Empty;
        }

        public void SetRuntimeEntries(params EndingTextEntry[] runtimeEntries)
        {
            entries.Clear();
            if (runtimeEntries == null || runtimeEntries.Length == 0)
            {
                return;
            }

            for (var i = 0; i < runtimeEntries.Length; i++)
            {
                if (runtimeEntries[i] != null)
                {
                    entries.Add(runtimeEntries[i]);
                }
            }
        }

        public static EndingDatabase CreateRuntimeDefault()
        {
            var database = CreateInstance<EndingDatabase>();
            database.hideFlags = HideFlags.DontSave;
            return database;
        }

        private void OnValidate()
        {
            var seen = new HashSet<EndingId>();
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry == null || entry.EndingId == EndingId.None)
                {
                    continue;
                }

                if (!seen.Add(entry.EndingId))
                {
                    Debug.LogWarning($"[EndingDatabase] Duplicate endingId detected: {entry.EndingId}", this);
                }
            }
        }
    }
}
