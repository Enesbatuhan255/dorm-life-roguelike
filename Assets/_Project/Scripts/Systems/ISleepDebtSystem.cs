using System;

namespace DormLifeRoguelike
{
    public interface ISleepDebtSystem : IService
    {
        event Action<float> OnSleepDebtChanged;

        float SleepDebt { get; }

        float GetEnergyCostMultiplier();

        float GetMentalCostMultiplier();

        void BeginSleep(int sleepHours);

        void ApplySleepAction(int sleepHours);

        void EndSleep();
    }
}
