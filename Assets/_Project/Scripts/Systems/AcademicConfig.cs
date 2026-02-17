using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "AcademicConfig", menuName = "DormLifeRoguelike/Config/AcademicConfig")]
    public sealed class AcademicConfig : ScriptableObject
    {
        [SerializeField] private float safeMin = 2f;
        [SerializeField] private float warningMin = 1.8f;
        [Min(1)]
        [SerializeField] private int criticalGraceDays = 7;
        [TextArea]
        [SerializeField] private string failEpilog = "Akademik ortalama kritik seviyede kaldigi icin okuldan atildin.";

        public float SafeMin => safeMin;
        public float WarningMin => warningMin;
        public int CriticalGraceDays => criticalGraceDays;
        public string FailEpilog => failEpilog;

        public static AcademicConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<AcademicConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
