using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "WorkLimitConfig", menuName = "DormLifeRoguelike/Config/WorkLimitConfig")]
    public sealed class WorkLimitConfig : ScriptableObject
    {
        [Min(1)]
        [SerializeField] private int maxWorkActionsPerWeek = 3;

        public int MaxWorkActionsPerWeek => maxWorkActionsPerWeek;

        public static WorkLimitConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<WorkLimitConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
