#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DormLifeRoguelike;
using DormLifeRoguelike.Editor;
using NUnit.Framework;
using UnityEditor;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventChoicePolicyTests
    {
        private static readonly string[] Wave1Ids =
        {
            "EVT_MAJOR_DEBT_001",
            "EVT_MAJOR_DEBT_002",
            "EVT_MAJOR_KYK_WARNING_001",
            "EVT_MAJOR_KYK_MONITOR_001",
            "EVT_MAJOR_KYK_CUT_001",
            "EVT_MINOR_GAMBLE_001",
            "EVT_MAJOR_GAMBLE_002",
            "EVT_MAJOR_GAMBLE_003",
            "EVT_MAJOR_INFLATION_001",
            "EVT_MAJOR_WORK_001",
            "EVT_MAJOR_SLEEP_001",
            "EVT_MAJOR_FAMILY_001",
            "EVT_MAJOR_ACADEMIC_001",
            "EVT_MINOR_COST_001",
            "EVT_MINOR_COST_003",
            "EVT_MINOR_HEALTH_001"
        };

        [Test]
        public void Wave1Events_MatchChoicePolicy_ByCategory()
        {
            var byId = LoadEventMap();

            for (var i = 0; i < Wave1Ids.Length; i++)
            {
                var id = Wave1Ids[i];
                Assert.That(byId.ContainsKey(id), Is.True, $"Missing event asset for id '{id}'.");

                var evt = byId[id];
                var count = evt.Choices == null ? 0 : evt.Choices.Count;
                if (evt.Category == "Major")
                {
                    Assert.That(count, Is.GreaterThanOrEqualTo(3), $"{id} must have >=3 choices.");
                }
                else
                {
                    Assert.That(count, Is.GreaterThanOrEqualTo(2), $"{id} must have >=2 choices.");
                }
            }
        }

        [Test]
        public void ChoiceCoverageValidator_Wave1HasNoViolations()
        {
            var result = EventChoiceCoverageValidator.Validate();
            var wave1ViolationCount = result.Violations.Count(v => Wave1Ids.Contains(v.EventId));
            Assert.That(wave1ViolationCount, Is.EqualTo(0));
        }

        private static Dictionary<string, EventData> LoadEventMap()
        {
            var map = new Dictionary<string, EventData>();
            var guids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events" });
            for (var i = 0; i < guids.Length; i++)
            {
                var evt = AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (evt == null || string.IsNullOrWhiteSpace(evt.EventId))
                {
                    continue;
                }

                map[evt.EventId.Trim()] = evt;
            }

            return map;
        }
    }
}
#endif
