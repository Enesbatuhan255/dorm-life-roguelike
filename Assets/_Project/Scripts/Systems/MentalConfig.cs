using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "MentalConfig", menuName = "DormLifeRoguelike/Config/MentalConfig")]
    public sealed class MentalConfig : ScriptableObject
    {
        [Header("Study Energy Multipliers")]
        [SerializeField] private float highMentalMin = 70f;
        [SerializeField] private float mediumMentalMin = 40f;
        [SerializeField] private float highMentalEnergyMultiplier = 0.7f;
        [SerializeField] private float mediumMentalEnergyMultiplier = 1f;
        [SerializeField] private float lowMentalEnergyMultiplier = 1.5f;

        [Header("Overdrive")]
        [SerializeField] private float energyFloor = -10f;
        [SerializeField] private float mildOverdriveMin = -5f;
        [SerializeField] private float mildOverdriveMentalPenalty = -2f;
        [SerializeField] private float severeOverdriveMentalPenalty = -5f;
        [Min(0)]
        [SerializeField] private int severeOverdriveEnergyPenaltyDays = 1;
        [SerializeField] private float severeOverdriveDailyEnergyPenalty = -10f;

        [Header("Socialize")]
        [SerializeField] private float socializeEnergyCost = -2f;
        [SerializeField] private float socializeMentalGain = 4f;
        [SerializeField] private float socializeLowMentalThreshold = 20f;
        [SerializeField] private float socializeLowMentalGain = 2f;

        public float HighMentalMin => highMentalMin;
        public float MediumMentalMin => mediumMentalMin;
        public float HighMentalEnergyMultiplier => highMentalEnergyMultiplier;
        public float MediumMentalEnergyMultiplier => mediumMentalEnergyMultiplier;
        public float LowMentalEnergyMultiplier => lowMentalEnergyMultiplier;
        public float EnergyFloor => energyFloor;
        public float MildOverdriveMin => mildOverdriveMin;
        public float MildOverdriveMentalPenalty => mildOverdriveMentalPenalty;
        public float SevereOverdriveMentalPenalty => severeOverdriveMentalPenalty;
        public int SevereOverdriveEnergyPenaltyDays => severeOverdriveEnergyPenaltyDays;
        public float SevereOverdriveDailyEnergyPenalty => severeOverdriveDailyEnergyPenalty;
        public float SocializeEnergyCost => socializeEnergyCost;
        public float SocializeMentalGain => socializeMentalGain;
        public float SocializeLowMentalThreshold => socializeLowMentalThreshold;
        public float SocializeLowMentalGain => socializeLowMentalGain;

        public float GetStudyEnergyMultiplier(float currentMental)
        {
            if (currentMental >= highMentalMin)
            {
                return highMentalEnergyMultiplier;
            }

            if (currentMental >= mediumMentalMin)
            {
                return mediumMentalEnergyMultiplier;
            }

            return lowMentalEnergyMultiplier;
        }

        public static MentalConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<MentalConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
