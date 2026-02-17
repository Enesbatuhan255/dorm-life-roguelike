using UnityEngine;

namespace DormLifeRoguelike
{
    public enum GameOutcomeFailPriority
    {
        MentalFirst,
        MoneyFirst,
        AcademicFirst,
        EnergyFirst
    }

    [CreateAssetMenu(fileName = "GameOutcomeConfig", menuName = "DormLifeRoguelike/Config/GameOutcomeConfig")]
    public sealed class GameOutcomeConfig : ScriptableObject
    {
        [Header("Timeline")]
        [Min(1)]
        [SerializeField] private int targetDays = 7;

        [Header("Pass Rules")]
        [SerializeField] private float minAcademicPass = 70f;

        [Header("Fail Rules")]
        [SerializeField] private bool useAcademicFailThreshold = true;
        [SerializeField] private float minAcademicFail = 40f;
        [SerializeField] private float minMental = 20f;
        [SerializeField] private bool useEnergyThreshold;
        [SerializeField] private float minEnergy = 10f;
        [SerializeField] private float minMoney = -500f;
        [SerializeField] private GameOutcomeFailPriority failPriority = GameOutcomeFailPriority.MentalFirst;

        [Header("Epilog Thresholds")]
        [SerializeField] private float severeDebtThreshold = -1500f;
        [SerializeField] private float debtThreshold = -1000f;
        [SerializeField] private float lightDebtThreshold = -200f;
        [SerializeField] private float lowMentalThreshold = 25f;
        [SerializeField] private float fragileMentalThreshold = 45f;
        [SerializeField] private float lowEnergyThreshold = 20f;

        public int TargetDays => targetDays;

        public float MinAcademicPass => minAcademicPass;

        public bool UseAcademicFailThreshold => useAcademicFailThreshold;

        public float MinAcademicFail => minAcademicFail;

        public float MinMental => minMental;

        public bool UseEnergyThreshold => useEnergyThreshold;

        public float MinEnergy => minEnergy;

        public float MinMoney => minMoney;

        public GameOutcomeFailPriority FailPriority => failPriority;

        public float SevereDebtThreshold => severeDebtThreshold;

        public float DebtThreshold => debtThreshold;

        public float LightDebtThreshold => lightDebtThreshold;

        public float LowMentalThreshold => lowMentalThreshold;

        public float FragileMentalThreshold => fragileMentalThreshold;

        public float LowEnergyThreshold => lowEnergyThreshold;

        public void SetRuntimeValues(
            int targetDaysValue,
            float minAcademicPassValue,
            bool useAcademicFailThresholdValue,
            float minAcademicFailValue,
            float minMentalValue,
            bool useEnergyThresholdValue,
            float minEnergyValue,
            float minMoneyValue,
            GameOutcomeFailPriority failPriorityValue,
            float severeDebtThresholdValue = -1500f,
            float debtThresholdValue = -1000f,
            float lightDebtThresholdValue = -200f,
            float lowMentalThresholdValue = 25f,
            float fragileMentalThresholdValue = 45f,
            float lowEnergyThresholdValue = 20f)
        {
            targetDays = Mathf.Max(1, targetDaysValue);
            minAcademicPass = minAcademicPassValue;
            useAcademicFailThreshold = useAcademicFailThresholdValue;
            minAcademicFail = minAcademicFailValue;
            minMental = minMentalValue;
            useEnergyThreshold = useEnergyThresholdValue;
            minEnergy = minEnergyValue;
            minMoney = minMoneyValue;
            failPriority = failPriorityValue;
            severeDebtThreshold = severeDebtThresholdValue;
            debtThreshold = debtThresholdValue;
            lightDebtThreshold = lightDebtThresholdValue;
            lowMentalThreshold = lowMentalThresholdValue;
            fragileMentalThreshold = fragileMentalThresholdValue;
            lowEnergyThreshold = lowEnergyThresholdValue;
        }

        public static GameOutcomeConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<GameOutcomeConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
