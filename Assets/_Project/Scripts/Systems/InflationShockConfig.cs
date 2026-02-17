using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "InflationShockConfig", menuName = "DormLifeRoguelike/Config/InflationShockConfig")]
    public sealed class InflationShockConfig : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int triggerDay = 37;
        [Min(1f)]
        [SerializeField] private float multiplier = 1.2f;

        public int TriggerDay => triggerDay;
        public float Multiplier => multiplier;

        public static InflationShockConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<InflationShockConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
