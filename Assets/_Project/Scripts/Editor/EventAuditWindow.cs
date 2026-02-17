using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DormLifeRoguelike.Editor
{
    public sealed class EventAuditWindow : EditorWindow
    {
        private const string CsvRelativePath = "Assets/_Project/Reports/event_audit_report.csv";
        private static readonly Regex MiscIdRegex = new Regex(@"^EVT_MISC_(\d{3})$", RegexOptions.Compiled);
        private static readonly Regex IdPatternRegex = new Regex(@"^EVT_[A-Z0-9_]+$", RegexOptions.Compiled);

        private readonly List<AuditIssue> issues = new List<AuditIssue>();
        private readonly List<EventAssetRecord> records = new List<EventAssetRecord>();
        private Vector2 scrollPosition;
        private string lastExportPath = "Not exported yet";
        private DateTime? lastExportTimeUtc;

        [MenuItem("Tools/DormLifeRoguelike/Event Audit")]
        public static void Open()
        {
            var window = GetWindow<EventAuditWindow>("Event Audit");
            window.minSize = new Vector2(900f, 520f);
            window.RunAudit();
        }

        private void OnGUI()
        {
            DrawToolbar();
            DrawSummary();
            DrawExportInfo();
            DrawIssues();
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Run Audit", GUILayout.Height(28f)))
                {
                    RunAudit();
                }

                if (GUILayout.Button("Auto-Fix Missing IDs", GUILayout.Height(28f)))
                {
                    AutoFixMissingIds();
                }

                if (GUILayout.Button("Export CSV", GUILayout.Height(28f)))
                {
                    ExportCsv();
                }
            }
        }

        private void DrawSummary()
        {
            var errorCount = 0;
            var warningCount = 0;
            for (var i = 0; i < issues.Count; i++)
            {
                if (issues[i].Severity == IssueSeverity.Error)
                {
                    errorCount++;
                }
                else
                {
                    warningCount++;
                }
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField(
                $"Assets: {records.Count}    Issues: {issues.Count}    Errors: {errorCount}    Warnings: {warningCount}",
                EditorStyles.boldLabel);
        }

        private void DrawExportInfo()
        {
            EditorGUILayout.Space(2f);
            EditorGUILayout.LabelField($"Last export path: {lastExportPath}");
            var timestamp = lastExportTimeUtc.HasValue
                ? lastExportTimeUtc.Value.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")
                : "Never";
            EditorGUILayout.LabelField($"Last export time: {timestamp}");
            EditorGUILayout.Space(6f);
        }

        private void DrawIssues()
        {
            using var scroll = new EditorGUILayout.ScrollViewScope(scrollPosition);
            scrollPosition = scroll.scrollPosition;

            if (issues.Count == 0)
            {
                EditorGUILayout.HelpBox("No issues found.", MessageType.Info);
                return;
            }

            for (var i = 0; i < issues.Count; i++)
            {
                var issue = issues[i];
                var type = issue.Severity == IssueSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.HelpBox(
                    $"{issue.Severity.ToString().ToUpperInvariant()} [{issue.IssueCode}] {issue.EventId} @ {issue.AssetPath}\n{issue.Message}",
                    type);
            }
        }

        private List<EventAssetRecord> CollectAllEventData()
        {
            var result = new List<EventAssetRecord>();
            var guids = AssetDatabase.FindAssets("t:EventData");
            for (var i = 0; i < guids.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (asset != null)
                {
                    result.Add(new EventAssetRecord(asset, path));
                }
            }

            return result;
        }

        private void RunAudit()
        {
            records.Clear();
            records.AddRange(CollectAllEventData());

            issues.Clear();
            AddIdAndMetadataIssues(records, issues);
            AddContentIssues(records, issues);
            Repaint();
        }

        private static void AddIdAndMetadataIssues(List<EventAssetRecord> source, List<AuditIssue> output)
        {
            var idBuckets = new Dictionary<string, List<EventAssetRecord>>();

            for (var i = 0; i < source.Count; i++)
            {
                var record = source[i];
                var eventId = Normalize(record.Asset.EventId);
                var title = Normalize(record.Asset.Title);
                var description = Normalize(record.Asset.Description);

                if (string.IsNullOrEmpty(eventId))
                {
                    output.Add(new AuditIssue(
                        IssueSeverity.Error,
                        string.Empty,
                        record.AssetPath,
                        "MISSING_ID",
                        "eventId is empty."));
                }
                else
                {
                    if (!IdPatternRegex.IsMatch(eventId))
                    {
                        output.Add(new AuditIssue(
                            IssueSeverity.Warning,
                            eventId,
                            record.AssetPath,
                            "BAD_PATTERN",
                            "eventId should match pattern EVT_[A-Z0-9_]+."));
                    }

                    if (!idBuckets.TryGetValue(eventId, out var list))
                    {
                        list = new List<EventAssetRecord>();
                        idBuckets[eventId] = list;
                    }

                    list.Add(record);
                }

                if (string.IsNullOrEmpty(title))
                {
                    output.Add(new AuditIssue(
                        IssueSeverity.Error,
                        eventId,
                        record.AssetPath,
                        "MISSING_TITLE",
                        "Title is empty."));
                }

                if (string.IsNullOrEmpty(description))
                {
                    output.Add(new AuditIssue(
                        IssueSeverity.Warning,
                        eventId,
                        record.AssetPath,
                        "MISSING_DESC",
                        "Description is empty."));
                }
            }

            foreach (var pair in idBuckets)
            {
                if (pair.Value.Count <= 1)
                {
                    continue;
                }

                for (var i = 0; i < pair.Value.Count; i++)
                {
                    output.Add(new AuditIssue(
                        IssueSeverity.Error,
                        pair.Key,
                        pair.Value[i].AssetPath,
                        "DUPLICATE_ID",
                        $"Duplicate eventId '{pair.Key}' appears {pair.Value.Count} times."));
                }
            }
        }

        private static void AddContentIssues(List<EventAssetRecord> source, List<AuditIssue> output)
        {
            for (var i = 0; i < source.Count; i++)
            {
                var record = source[i];
                var asset = record.Asset;
                var eventId = Normalize(asset.EventId);
                var choices = asset.Choices;

                if (choices == null || choices.Count == 0)
                {
                    output.Add(new AuditIssue(
                        IssueSeverity.Error,
                        eventId,
                        record.AssetPath,
                        "BAD_CHOICE_COUNT",
                        "Event has no choices."));
                    continue;
                }

                for (var c = 0; c < choices.Count; c++)
                {
                    var choice = choices[c];
                    if (choice == null)
                    {
                        output.Add(new AuditIssue(
                            IssueSeverity.Warning,
                            eventId,
                            record.AssetPath,
                            "BAD_CHOICE_COUNT",
                            $"Choice index {c} is null."));
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(choice.Text))
                    {
                        output.Add(new AuditIssue(
                            IssueSeverity.Warning,
                            eventId,
                            record.AssetPath,
                            "EMPTY_CHOICE_TEXT",
                            $"Choice index {c} has empty text."));
                    }

                    var effects = choice.Effects;
                    if (effects == null || effects.Count == 0)
                    {
                        output.Add(new AuditIssue(
                            IssueSeverity.Warning,
                            eventId,
                            record.AssetPath,
                            "EMPTY_EFFECTS",
                            $"Choice index {c} has no effects."));
                    }
                    else
                    {
                        for (var e = 0; e < effects.Count; e++)
                        {
                            var effect = effects[e];
                            if (effect == null)
                            {
                                continue;
                            }

                            if (float.IsNaN(effect.Delta) || float.IsInfinity(effect.Delta))
                            {
                                output.Add(new AuditIssue(
                                    IssueSeverity.Error,
                                    eventId,
                                    record.AssetPath,
                                    "SUSPICIOUS_DELTA",
                                    $"Choice {c} effect {e} has invalid delta (NaN/Infinity)."));
                                continue;
                            }

                            if (Mathf.Abs(effect.Delta) > 50f)
                            {
                                output.Add(new AuditIssue(
                                    IssueSeverity.Warning,
                                    eventId,
                                    record.AssetPath,
                                    "SUSPICIOUS_DELTA",
                                    $"Choice {c} effect {e} has large delta ({effect.Delta:0.##})."));
                            }
                        }
                    }

                    var condition = choice.Condition;
                    if (condition == null || !condition.IsEnabled)
                    {
                        continue;
                    }

                    if (float.IsNaN(condition.Value) || float.IsInfinity(condition.Value))
                    {
                        output.Add(new AuditIssue(
                            IssueSeverity.Error,
                            eventId,
                            record.AssetPath,
                            "INVALID_CONDITION",
                            $"Choice {c} condition has invalid value (NaN/Infinity)."));
                    }
                }
            }
        }

        private void AutoFixMissingIds()
        {
            var all = CollectAllEventData();
            var existingIds = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i < all.Count; i++)
            {
                var id = Normalize(all[i].Asset.EventId);
                if (!string.IsNullOrEmpty(id))
                {
                    existingIds.Add(id);
                }
            }

            var fixedCount = 0;
            var summary = new StringBuilder();
            var nextMiscId = GenerateNextMiscId(existingIds);

            for (var i = 0; i < all.Count; i++)
            {
                var asset = all[i].Asset;
                var current = Normalize(asset.EventId);
                if (!string.IsNullOrEmpty(current))
                {
                    continue;
                }

                while (existingIds.Contains(nextMiscId))
                {
                    nextMiscId = GenerateNextMiscId(existingIds);
                }

                var serialized = new SerializedObject(asset);
                var prop = serialized.FindProperty("eventId");
                if (prop == null)
                {
                    continue;
                }

                prop.stringValue = nextMiscId;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);

                existingIds.Add(nextMiscId);
                fixedCount++;
                summary.AppendLine($"{all[i].AssetPath} -> {nextMiscId}");

                nextMiscId = GenerateNextMiscId(existingIds);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RunAudit();

            if (fixedCount <= 0)
            {
                Debug.Log("[EventAudit] Auto-fix completed. No missing IDs found.");
                return;
            }

            Debug.Log($"[EventAudit] Auto-fix completed. Fixed {fixedCount} assets.\n{summary}");
        }

        private static string GenerateNextMiscId(HashSet<string> existingIds)
        {
            var maxNumber = 0;
            foreach (var id in existingIds)
            {
                var match = MiscIdRegex.Match(id);
                if (!match.Success)
                {
                    continue;
                }

                if (int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                    && parsed > maxNumber)
                {
                    maxNumber = parsed;
                }
            }

            return $"EVT_MISC_{(maxNumber + 1):D3}";
        }

        private void ExportCsv()
        {
            RunAudit();

            var absolutePath = Path.GetFullPath(CsvRelativePath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            WriteCsv(issues, absolutePath);
            AssetDatabase.Refresh();

            lastExportPath = CsvRelativePath;
            lastExportTimeUtc = DateTime.UtcNow;

            Debug.Log($"[EventAudit] CSV exported: {CsvRelativePath}");
            Repaint();
        }

        private static void WriteCsv(IReadOnlyList<AuditIssue> rows, string outputPath)
        {
            var sb = new StringBuilder();
            sb.AppendLine("severity,eventId,assetPath,issueCode,message");
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                sb.Append(EscapeCsv(row.Severity == IssueSeverity.Error ? "ERROR" : "WARNING")).Append(",");
                sb.Append(EscapeCsv(row.EventId)).Append(",");
                sb.Append(EscapeCsv(row.AssetPath)).Append(",");
                sb.Append(EscapeCsv(row.IssueCode)).Append(",");
                sb.Append(EscapeCsv(row.Message)).AppendLine();
            }

            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(false));
        }

        private static string EscapeCsv(string value)
        {
            var safe = value ?? string.Empty;
            var escaped = safe.Replace("\"", "\"\"");
            return $"\"{escaped}\"";
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private readonly struct EventAssetRecord
        {
            public EventAssetRecord(EventData asset, string assetPath)
            {
                Asset = asset;
                AssetPath = assetPath;
            }

            public EventData Asset { get; }
            public string AssetPath { get; }
        }

        private readonly struct AuditIssue
        {
            public AuditIssue(IssueSeverity severity, string eventId, string assetPath, string issueCode, string message)
            {
                Severity = severity;
                EventId = eventId ?? string.Empty;
                AssetPath = assetPath ?? string.Empty;
                IssueCode = issueCode ?? string.Empty;
                Message = message ?? string.Empty;
            }

            public IssueSeverity Severity { get; }
            public string EventId { get; }
            public string AssetPath { get; }
            public string IssueCode { get; }
            public string Message { get; }
        }

        private enum IssueSeverity
        {
            Warning,
            Error
        }
    }
}
