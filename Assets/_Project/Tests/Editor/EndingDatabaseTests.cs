using NUnit.Framework;
using UnityEngine;

namespace DormLifeRoguelike.Tests.EditMode
{
    public sealed class EndingDatabaseTests
    {
        [Test]
        public void TryGetEntry_ReturnsConfiguredEntry()
        {
            var database = ScriptableObject.CreateInstance<EndingDatabase>();
            var entry = new EndingTextEntry();
            entry.SetRuntimeValues(EndingId.GraduatedResilient, "Resilient", "Body");
            database.SetRuntimeEntries(entry);

            var found = database.TryGetEntry(EndingId.GraduatedResilient, out var resolved);

            Assert.That(found, Is.True);
            Assert.That(resolved, Is.Not.Null);
            Assert.That(resolved.EpilogTitle, Is.EqualTo("Resilient"));
            Assert.That(resolved.EpilogBody, Is.EqualTo("Body"));

            Object.DestroyImmediate(database);
        }

        [Test]
        public void TryGetEntry_MissingEntry_ReturnsFalse()
        {
            var database = ScriptableObject.CreateInstance<EndingDatabase>();
            database.SetRuntimeEntries();

            var found = database.TryGetEntry(EndingId.FailedDebtTrap, out var resolved);

            Assert.That(found, Is.False);
            Assert.That(resolved, Is.Null);

            Object.DestroyImmediate(database);
        }
    }
}
