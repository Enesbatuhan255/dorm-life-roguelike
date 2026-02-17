using UnityEngine;

namespace DormLifeRoguelike
{
    [CreateAssetMenu(fileName = "MicroChallengeConfig", menuName = "DormLifeRoguelike/Config/MicroChallengeConfig")]
    public sealed class MicroChallengeConfig : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField] private float defaultTimeLimitSeconds = 8f;

        [Header("Outcome Thresholds")]
        [SerializeField] private float perfectScoreMin = 0.8f;
        [SerializeField] private float goodScoreMin = 0.45f;

        [Header("Mental Modifier")]
        [SerializeField] private float highMentalMin = 70f;
        [SerializeField] private float mediumMentalMin = 40f;
        [SerializeField] private float highMentalModifier = 0.08f;
        [SerializeField] private float lowMentalModifier = -0.1f;

        [Header("Energy Modifier")]
        [SerializeField] private float highEnergyMin = 50f;
        [SerializeField] private float highEnergyModifier = 0.05f;
        [SerializeField] private float negativeEnergyModifier = -0.12f;

        [Header("Work Outcome Deltas")]
        [SerializeField] private float workPerfectMoneyBonus = 15f;
        [SerializeField] private float workPoorMoneyPenalty = -10f;
        [SerializeField] private float workPoorMentalPenalty = -1f;

        public float DefaultTimeLimitSeconds => defaultTimeLimitSeconds;
        public float PerfectScoreMin => perfectScoreMin;
        public float GoodScoreMin => goodScoreMin;
        public float WorkPerfectMoneyBonus => workPerfectMoneyBonus;
        public float WorkPoorMoneyPenalty => workPoorMoneyPenalty;
        public float WorkPoorMentalPenalty => workPoorMentalPenalty;

        public float ResolveStatModifier(float mental, float energy)
        {
            var modifier = 0f;

            if (mental >= highMentalMin)
            {
                modifier += highMentalModifier;
            }
            else if (mental < mediumMentalMin)
            {
                modifier += lowMentalModifier;
            }

            if (energy >= highEnergyMin)
            {
                modifier += highEnergyModifier;
            }
            else if (energy < 0f)
            {
                modifier += negativeEnergyModifier;
            }

            return modifier;
        }

        public MicroChallengeOutcomeBand ResolveBand(float effectiveScore)
        {
            if (effectiveScore >= perfectScoreMin)
            {
                return MicroChallengeOutcomeBand.Perfect;
            }

            if (effectiveScore >= goodScoreMin)
            {
                return MicroChallengeOutcomeBand.Good;
            }

            return MicroChallengeOutcomeBand.Poor;
        }

        public static MicroChallengeConfig CreateRuntimeDefault()
        {
            var config = CreateInstance<MicroChallengeConfig>();
            config.hideFlags = HideFlags.DontSave;
            return config;
        }
    }
}
