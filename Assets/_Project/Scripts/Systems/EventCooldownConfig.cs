using System;
using System.Collections.Generic;
using UnityEngine;

namespace DormLifeRoguelike
{
    [Serializable]
    public sealed class CategoryCooldownOverride
    {
        [SerializeField] private string category = "General";
        [Min(0)]
        [SerializeField] private int cooldownHours;

        public string Category => category;
        public int CooldownHours => cooldownHours;

        public static CategoryCooldownOverride CreateRuntime(string categoryValue, int cooldownHoursValue)
        {
            var result = new CategoryCooldownOverride
            {
                category = categoryValue ?? string.Empty,
                cooldownHours = Mathf.Max(0, cooldownHoursValue)
            };

            return result;
        }
    }

    [Serializable]
    public sealed class EventCooldownOverride
    {
        [SerializeField] private string eventId = string.Empty;
        [Min(0)]
        [SerializeField] private int cooldownHours;

        public string EventId => eventId;
        public int CooldownHours => cooldownHours;

        public static EventCooldownOverride CreateRuntime(string eventIdValue, int cooldownHoursValue)
        {
            var result = new EventCooldownOverride
            {
                eventId = eventIdValue ?? string.Empty,
                cooldownHours = Mathf.Max(0, cooldownHoursValue)
            };

            return result;
        }
    }

    [CreateAssetMenu(fileName = "EventCooldownConfig", menuName = "DormLifeRoguelike/Config/EventCooldownConfig")]
    public sealed class EventCooldownConfig : ScriptableObject
    {
        [Min(0)]
        [SerializeField] private int defaultCooldownHours = 6;
        [SerializeField] private List<CategoryCooldownOverride> perCategoryOverrides = new List<CategoryCooldownOverride>();
        [SerializeField] private List<EventCooldownOverride> perEventOverrides = new List<EventCooldownOverride>();

        public int DefaultCooldownHours => defaultCooldownHours;

        public IReadOnlyList<CategoryCooldownOverride> PerCategoryOverrides => perCategoryOverrides;

        public IReadOnlyList<EventCooldownOverride> PerEventOverrides => perEventOverrides;

        public int GetCooldownHours(EventData eventData)
        {
            if (eventData == null)
            {
                return Mathf.Max(0, defaultCooldownHours);
            }

            var eventId = Normalize(eventData.EventId);
            if (!string.IsNullOrWhiteSpace(eventId))
            {
                for (var i = 0; i < perEventOverrides.Count; i++)
                {
                    var entry = perEventOverrides[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (Normalize(entry.EventId) == eventId)
                    {
                        return Mathf.Max(0, entry.CooldownHours);
                    }
                }
            }

            var category = Normalize(eventData.Category);
            if (!string.IsNullOrWhiteSpace(category))
            {
                for (var i = 0; i < perCategoryOverrides.Count; i++)
                {
                    var entry = perCategoryOverrides[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    if (Normalize(entry.Category) == category)
                    {
                        return Mathf.Max(0, entry.CooldownHours);
                    }
                }
            }

            return Mathf.Max(0, defaultCooldownHours);
        }

        public void SetRuntimeDefaults(int defaultCooldown)
        {
            defaultCooldownHours = Mathf.Max(0, defaultCooldown);
            perCategoryOverrides.Clear();
            perEventOverrides.Clear();
        }

        public void AddRuntimeCategoryOverride(string category, int cooldownHours)
        {
            perCategoryOverrides.Add(CategoryCooldownOverride.CreateRuntime(category, cooldownHours));
        }

        public void AddRuntimeEventOverride(string eventId, int cooldownHours)
        {
            perEventOverrides.Add(EventCooldownOverride.CreateRuntime(eventId, cooldownHours));
        }

        public static EventCooldownConfig CreateRuntimeDefault(int defaultCooldown)
        {
            var config = CreateInstance<EventCooldownConfig>();
            config.hideFlags = HideFlags.DontSave;
            config.SetRuntimeDefaults(defaultCooldown);
            return config;
        }

        private static string Normalize(string value)
        {
            return value == null ? string.Empty : value.Trim().ToLowerInvariant();
        }
    }
}
