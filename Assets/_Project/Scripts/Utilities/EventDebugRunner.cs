using UnityEngine;

namespace DormLifeRoguelike
{
    public sealed class EventDebugRunner : MonoBehaviour
    {
        [SerializeField] private EventData eventData;
        [SerializeField] private int choiceIndex;
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool autoApplyChoiceAfterEnqueue;

        public EventData ConfiguredEventData => eventData;

        private void Start()
        {
            if (runOnStart)
            {
                Run();
            }
        }

        [ContextMenu("Queue Event")]
        public void Run()
        {
            if (eventData == null)
            {
                Debug.LogWarning("[EventDebugRunner] EventData is not assigned.");
                return;
            }

            if (!ServiceLocator.TryGet<IEventManager>(out var eventManager))
            {
                Debug.LogError("[EventDebugRunner] IEventManager service not found.");
                return;
            }

            eventManager.EnqueueEvent(eventData);

            Debug.Log(
                "[EventDebugRunner] Event queued: '" + eventData.Title + "'" +
                " | Pending=" + eventManager.HasPendingEvents +
                " | Current=" + (eventManager.CurrentEvent == null ? "None" : eventManager.CurrentEvent.Title));

            if (!autoApplyChoiceAfterEnqueue)
            {
                return;
            }

            if (!ServiceLocator.TryGet<IStatSystem>(out var statSystem))
            {
                Debug.LogError("[EventDebugRunner] IStatSystem service not found.");
                return;
            }

            var targetEvent = eventManager.CurrentEvent;
            if (targetEvent == null)
            {
                Debug.LogWarning("[EventDebugRunner] No active event to auto-apply.");
                return;
            }

            var beforeEnergy = statSystem.GetStat(StatType.Energy);
            var beforeAcademic = statSystem.GetStat(StatType.Academic);

            var success = eventManager.TryApplyChoice(targetEvent, choiceIndex, out var outcomeMessage);

            var afterEnergy = statSystem.GetStat(StatType.Energy);
            var afterAcademic = statSystem.GetStat(StatType.Academic);

            Debug.Log(
                "[EventDebugRunner] Success=" + success +
                " | ChoiceIndex=" + choiceIndex +
                " | Outcome='" + outcomeMessage + "'" +
                " | Energy: " + beforeEnergy + " -> " + afterEnergy +
                " | Academic: " + beforeAcademic + " -> " + afterAcademic);
        }
    }
}
