#if UNITY_EDITOR
using System.Linq;
using DormLifeRoguelike;
using NUnit.Framework;
using UnityEditor;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EventBibleCoverageTests
    {
        [Test]
        public void EventBible_HasExpectedMinorAndMajorCounts()
        {
            var minorGuids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events/Minor" });
            var majorGuids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events/Major" });

            Assert.That(minorGuids.Length, Is.EqualTo(12));
            Assert.That(majorGuids.Length, Is.EqualTo(16));
        }

        [Test]
        public void EventBible_AllCategoriesAreCorrectAndIdsUnique()
        {
            var allGuids = AssetDatabase.FindAssets("t:EventData", new[] { "Assets/_Project/ScriptableObjects/Events" });
            var eventData = allGuids
                .Select(g => AssetDatabase.LoadAssetAtPath<EventData>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(e => e != null)
                .ToArray();

            Assert.That(eventData.Length, Is.EqualTo(28));
            Assert.That(eventData.Count(e => e.Category == "Minor"), Is.EqualTo(12));
            Assert.That(eventData.Count(e => e.Category == "Major"), Is.EqualTo(16));

            var distinctIds = eventData.Select(e => e.EventId).Distinct().Count();
            Assert.That(distinctIds, Is.EqualTo(28));
        }
    }
}
#endif
