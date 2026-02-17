using System;

namespace DormLifeRoguelike
{
    public interface IEconomySystem : IService
    {
        event Action<float, string> OnTransactionApplied;

        bool CanAfford(float cost);

        void ApplyTransaction(float amount, string reason);

        void ApplyDailyCosts();
    }
}
