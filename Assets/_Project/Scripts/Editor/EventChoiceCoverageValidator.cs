#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DormLifeRoguelike.Editor
{
    public static class EventChoiceCoverageValidator
    {
        private const string EventsRoot = "Assets/_Project/ScriptableObjects/Events";
        private const string ReportPath = "Assets/_Project/Reports/event_choice_coverage_report.csv";

        [MenuItem("Tools/DormLifeRoguelike/Validate Choice Coverage")]
        public static void ValidateAndLog()
        {
            var result = Validate();
            ExportCsv(result);

            if (result.Violations.Count == 0)
            {
                Debug.Log($"[ChoiceCoverage] OK. Checked {result.TotalEvents} events. Report: {ReportPath}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[ChoiceCoverage] Found {result.Violations.Count} violation(s).");
            for (var i = 0; i < result.Violations.Count; i++)
            {
                var v = result.Violations[i];
                sb.AppendLine($"- {v.EventId} @ {v.AssetPath}: {v.Message}");
            }

            Debug.LogWarning(sb.ToString());
        }

        public static ChoiceCoverageResult Validate()
        {
            var events = LoadEvents();
            var violations = new List<ChoiceCoverageViolation>();
            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                var required = GetRequiredChoiceCount(evt.Category);
                var actual = evt.Choices == null ? 0 : evt.Choices.Count;
                if (actual < required)
                {
                    violations.Add(new ChoiceCoverageViolation(
                        evt.EventId,
                        AssetDatabase.GetAssetPath(evt),
                        $"Expected >= {required} choices for category '{evt.Category}', got {actual}."));
                }
            }

            return new ChoiceCoverageResult(events.Count, violations);
        }

        public static void ExportCsv(ChoiceCoverageResult result)
        {
            var directory = Path.GetDirectoryName(ReportPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var events = LoadEvents();
            var rows = new List<string>(events.Count + 1)
            {
                "eventId,category,choiceCount,requiredChoiceCount,status,assetPath"
            };

            for (var i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                var required = GetRequiredChoiceCount(evt.Category);
                var actual = evt.Choices == null ? 0 : evt.Choices.Count;
                var status = actual >= required ? "OK" : "VIOLATION";
                rows.Add(string.Join(",",
                    Safe(evt.EventId),
                    Safe(evt.Category),
                    actual.ToString(),
                    required.ToString(),
                    status,
                    Safe(AssetDatabase.GetAssetPath(evt))));
            }

            File.WriteAllLines(ReportPath, rows);
            AssetDatabase.Refresh();
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

        private static int GetRequiredChoiceCount(string category)
        {
            return string.Equals(category?.Trim(), "Major", StringComparison.OrdinalIgnoreCase) ? 3 : 2;
        }

        private static string Safe(string value)
        {
            return string.IsNullOrEmpty(value) ? "\"\"" : $"\"{value.Replace("\"", "\"\"")}\"";
        }
    }

    public sealed class ChoiceCoverageResult
    {
        public ChoiceCoverageResult(int totalEvents, List<ChoiceCoverageViolation> violations)
        {
            TotalEvents = totalEvents;
            Violations = violations ?? new List<ChoiceCoverageViolation>();
        }

        public int TotalEvents { get; }
        public List<ChoiceCoverageViolation> Violations { get; }
    }

    public sealed class ChoiceCoverageViolation
    {
        public ChoiceCoverageViolation(string eventId, string assetPath, string message)
        {
            EventId = eventId ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string EventId { get; }
        public string AssetPath { get; }
        public string Message { get; }
    }
}
#endif
