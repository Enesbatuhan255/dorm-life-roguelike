namespace DormLifeRoguelike
{
    public static class StudyCostCalculator
    {
        public static float CalculateEnergyDelta(float baseEnergyCost, float currentMental, MentalConfig config)
        {
            if (baseEnergyCost >= 0f || config == null)
            {
                return baseEnergyCost;
            }

            var multiplier = config.GetStudyEnergyMultiplier(currentMental);
            return baseEnergyCost * multiplier;
        }
    }
}
