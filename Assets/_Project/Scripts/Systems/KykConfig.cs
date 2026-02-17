using UnityEngine;

namespace DormLifeRoguelike
{
    public enum KykStatus
    {
        Normal,
        Monitoring,
        Warning,
        Cut
    }

    [CreateAssetMenu(fileName = "KykConfig", menuName = "DormLifeRoguelike/Config/KykConfig")]
    public sealed class KykConfig : ScriptableObject
    {
        [SerializeField] private float safeAcademicMin = 2f;
        [SerializeField] private float criticalAcademicMin = 1.8f;
        [Min(1)]
        [SerializeField] private int monitoringDaysToWarning = 3;
        [SerializeField] private float monthlyPaymentAmount = 1500f;
        [SerializeField] private float payoutMentalGain = 3f;
        [SerializeField] private float cutMentalPenalty = -20f;

        public float SafeAcademicMin => safeAcademicMin;
        public float CriticalAcademicMin => criticalAcademicMin;
        public int MonitoringDaysToWarning => monitoringDaysToWarning;
        public float MonthlyPaymentAmount => monthlyPaymentAmount;
        public float PayoutMentalGain => payoutMentalGain;
        public float CutMentalPenalty => cutMentalPenalty;

        public static KykConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<KykConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
