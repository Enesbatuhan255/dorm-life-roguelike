using System;
using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class EconomySystem : IEconomySystem, IDisposable
    {
        private readonly IStatSystem statSystem;
        private readonly ITimeManager timeManager;
        private readonly IInflationShockSystem inflationShockSystem;
        private bool isDisposed;

        public EconomySystem(IStatSystem statSystem, ITimeManager timeManager)
            : this(statSystem, timeManager, null)
        {
        }

        public EconomySystem(IStatSystem statSystem, ITimeManager timeManager, IInflationShockSystem inflationShockSystem)
        {
            this.statSystem = statSystem ?? throw new ArgumentNullException(nameof(statSystem));
            this.timeManager = timeManager ?? throw new ArgumentNullException(nameof(timeManager));
            this.inflationShockSystem = inflationShockSystem;
            this.timeManager.OnDayChanged += HandleDayChanged;
        }

        public event Action<float, string> OnTransactionApplied;

        public bool CanAfford(float cost)
        {
            var required = Mathf.Max(0f, cost);
            var currentMoney = statSystem.GetStat(StatType.Money);
            return currentMoney >= required;
        }

        public void ApplyTransaction(float amount, string reason)
        {
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Transaction" : reason;
            statSystem.ApplyBaseDelta(StatType.Money, amount);
            OnTransactionApplied?.Invoke(amount, normalizedReason);
        }

        public void ApplyDailyCosts()
        {
            var total = 0f;

            var foodCost = ApplyInflation(-UnityEngine.Random.Range(10f, 30.0001f));
            ApplyTransaction(foodCost, "Food");
            total += foodCost;

            var transportCost = ApplyInflation(-5f);
            ApplyTransaction(transportCost, "Transport");
            total += transportCost;

            if (UnityEngine.Random.value < 0.2f)
            {
                var unexpectedExpense = ApplyInflation(-15f);
                ApplyTransaction(unexpectedExpense, "Unexpected expense");
                total += unexpectedExpense;
            }

            Debug.Log($"[EconomySystem] Daily costs applied. Total: {total:0.#}");
        }

        public void Dispose()
        {
            if (isDisposed)
            {
                return;
            }

            isDisposed = true;
            timeManager.OnDayChanged -= HandleDayChanged;
        }

        private void HandleDayChanged(int _)
        {
            ApplyDailyCosts();
        }

        private float ApplyInflation(float amount)
        {
            if (inflationShockSystem == null)
            {
                return amount;
            }

            return inflationShockSystem.ApplyToCost(amount);
        }
    }
}
