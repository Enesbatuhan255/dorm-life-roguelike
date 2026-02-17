#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace DormLifeRoguelike.Editor
{
    public static class EventChainGraphReport
    {
        private const string EventsRoot = "Assets/_Project/ScriptableObjects/Events";
        private const string ReportPath = "Assets/_Project/Reports/event_chain_graph.csv";

        [MenuItem("Tools/DormLifeRoguelike/Export Event Chain Graph")]
        public static void Export()
        {
            var events = LoadEvents();
            var validIds = new HashSet<string>(events.Select(e => e.EventId));
            var rows = new List<string> { "sourceEventId,sourceType,sourceChoiceIndex,targetEventId,delayDays,targetExists" };

            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt == null)
                {
                    continue;
                }

                AppendEventFollowUps(rows, evt, validIds);
                AppendChoiceFollowUps(rows, evt, validIds);
            }

            var directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllLines(ReportPath, rows);
            AssetDatabase.Refresh();
            Debug.Log($"[EventChainGraph] Exported {rows.Count - 1} edge(s) -> {ReportPath}");
        }

        private static void AppendEventFollowUps(List<string> rows, EventData evt, HashSet<string> validIds)
        {
            var list = evt.FollowUpEventIds;
            if (list == null)
            {
                return;
            }

            for (var i = 0; i < list.Count; i++)
            {
                var target = Normalize(list[i]);
                if (string.IsNullOrEmpty(target))
                {
                    continue;
                }

                rows.Add(string.Join(",",
                    Safe(evt.EventId),
                    "\"event\"",
                    -1,
                    Safe(target),
                    evt.FollowUpDelayDays,
                    validIds.Contains(target) ? "1" : "0"));
            }
        }

        private static void AppendChoiceFollowUps(List<string> rows, EventData evt, HashSet<string> validIds)
        {
            var choices = evt.Choices;
            if (choices == null)
            {
                return;
            }

            for (var c = 0; c < choices.Count; c++)
            {
                var choice = choices[c];
                var followUps = choice?.FollowUpEventIds;
                if (followUps == null)
                {
                    continue;
                }

                for (var i = 0; i < followUps.Count; i++)
                {
                    var target = Normalize(followUps[i]);
                    if (string.IsNullOrEmpty(target))
                    {
                        continue;
                    }

                    rows.Add(string.Join(",",
                        Safe(evt.EventId),
                        "\"choice\"",
                        c,
                        Safe(target),
                        choice.FollowUpDelayDays,
                        validIds.Contains(target) ? "1" : "0"));
                }
            }
        }

        private static List<EventData> LoadEvents()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { EventsRoot });
            return guids
                .Select(g => AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e != null)
                .OrderBy(e => e.EventId)
                .ToList();
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "\"\"" : $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }
}
#endif
