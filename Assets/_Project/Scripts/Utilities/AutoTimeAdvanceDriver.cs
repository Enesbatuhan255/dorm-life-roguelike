using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class AutoTimeAdvanceDriver : MonoBehaviour
    {
        [SerializeField] private float secondsPerGameHour = 20f;
        [SerializeField] private int hoursPerTick = 1;
        [SerializeField] private bool pauseWhileEventActive = true;
        [SerializeField] private bool pauseAfterGameResolved = true;
        [SerializeField] private bool requireApplicationFocus = false;
        [SerializeField] private bool useUnscaledTime = true;

        private float elapsedSeconds;

        public void Configure(
            float intervalSeconds,
            int tickHours,
            bool pauseOnEvent,
            bool pauseOnResolved,
            bool requireFocus)
        {
            secondsPerGameHour = Mathf.Max(0.5f, intervalSeconds);
            hoursPerTick = Mathf.Max(1, tickHours);
            pauseWhileEventActive = pauseOnEvent;
            pauseAfterGameResolved = pauseOnResolved;
            requireApplicationFocus = requireFocus;
        }

        private void Update()
        {
            if (!CanAdvanceTime())
            {
                return;
            }

            elapsedSeconds += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            var interval = Mathf.Max(0.5f, secondsPerGameHour);
            if (elapsedSeconds < interval)
            {
                return;
            }

            var tickCount = Mathf.FloorToInt(elapsedSeconds / interval);
            elapsedSeconds -= tickCount * interval;
            tickCount = Mathf.Clamp(tickCount, 1, 4);

            if (!ServiceLocator.TryGet<ITimeManager>(out var timeManager))
            {
                return;
            }

            var hours = Mathf.Max(1, hoursPerTick);
            for (var i = 0; i < tickCount; i++)
            {
                timeManager.AdvanceTime(hours);
            }
        }

        private bool CanAdvanceTime()
        {
            if (requireApplicationFocus && !Application.isFocused)
            {
                return false;
            }

            if (!ServiceLocator.TryGet<ITimeManager>(out _))
            {
                return false;
            }

            if (pauseAfterGameResolved
                && ServiceLocator.TryGet<IGameOutcomeSystem>(out var gameOutcomeSystem)
                && gameOutcomeSystem.IsResolved)
            {
                return false;
            }

            if (pauseWhileEventActive
                && ServiceLocator.TryGet<IEventManager>(out var eventManager)
                && eventManager.CurrentEvent != null)
            {
                return false;
            }

            return true;
        }
    }
}
