using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "SleepDebtConfig", menuName = "DormLifeRoguelike/Sleep Debt Config")]
    public sealed class SleepDebtConfig : ScriptableObject
    {
        [Header("Debt Rules")]
        [Min(0f)]
        [SerializeField] private float nightHourDebtIncrease = 2f;
        [Min(0f)]
        [SerializeField] private float sleepDebtReductionPerHour = 6f;

        [Header("Sleep Recovery")]
        [Min(0f)]
        [SerializeField] private float sleepEnergyRecoveryPerHour = 8f;
        [Min(0f)]
        [SerializeField] private float sleepMentalRecoveryPerHour = 2f;

        public float NightHourDebtIncrease => nightHourDebtIncrease;

        public float SleepDebtReductionPerHour => sleepDebtReductionPerHour;

        public float SleepEnergyRecoveryPerHour => sleepEnergyRecoveryPerHour;

        public float SleepMentalRecoveryPerHour => sleepMentalRecoveryPerHour;

        public static SleepDebtConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<SleepDebtConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
