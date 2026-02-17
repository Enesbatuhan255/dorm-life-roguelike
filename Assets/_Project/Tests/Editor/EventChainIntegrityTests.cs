#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEditor;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventChainIntegrityTests
    {
        [Test]
        public void ProductionEventFollowUps_ResolveToExistingEventIds()
        {
            var all = LoadEvents();
            var ids = new HashSet<string>(all.Select(e => e.EventId));
            var missingTargets = new List<string>();

            for (var i = 0; i < all.Count; i++)
            {
                var evt = all[i];
                CollectMissingTargets(evt.EventId, evt.FollowUpEventIds, ids, missingTargets, "event");

                var choices = evt.Choices;
                if (choices == null)
                {
                    continue;
                }

                for (var c = 0; c < choices.Count; c++)
                {
                    CollectMissingTargets(evt.EventId, choices[c]?.FollowUpEventIds, ids, missingTargets, $"choice[{c}]");
                }
            }

            Assert.That(missingTargets, Is.Empty, string.Join("\n", missingTargets));
        }

        private static void CollectMissingTargets(
            string sourceId,
            IReadOnlyList<string> followUpIds,
            HashSet<string> knownIds,
            List<string> output,
            string sourceType)
        {
            if (followUpIds == null)
            {
                return;
            }

            for (var i = 0; i < followUpIds.Count; i++)
            {
                var target = followUpIds[i];
                if (string.IsNullOrWhiteSpace(target))
                {
                    continue;
                }

                var normalized = target.Trim();
                if (!knownIds.Contains(normalized))
                {
                    output.Add($"{sourceId} -> {normalized} ({sourceType})");
                }
            }
        }

        private static List<EventData> LoadEvents()
        {
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events" });
            var all = new List<EventData>(guids.Length);
            for (var i = 0; i < guids.Length; i++)
            {
                var evt = AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (evt != null && !string.IsNullOrWhiteSpace(evt.EventId))
                {
                    all.Add(evt);
                }
            }

            return all;
        }
    }
}
#endif
