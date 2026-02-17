using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DormLifeRoguelike
{
    public sealed class EventInputTrigger : MonoBehaviour
    {
        [SerializeField] private KeyCode triggerKey = KeyCode.Space;
        [SerializeField] private EventData eventData;
        [SerializeField] private bool queueOnStart;

        private void Start()
        {
            if (queueOnStart)
            {
                EnqueueConfiguredEvent();
            }
        }

        private void Update()
        {
            if (!IsTriggerPressed())
            {
                return;
            }

            EnqueueConfiguredEvent();
        }

        private void EnqueueConfiguredEvent()
        {
            if (eventData == null)
            {
                return;
            }

            if (!ServiceLocator.TryGet<IEventManager>(out var eventManager))
            {
                Debug.LogError("[EventInputTrigger] IEventManager service not found.");
                return;
            }

            eventManager.EnqueueEvent(eventData);
        }

        private bool IsTriggerPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return triggerKey switch
            {
                KeyCode.Space => keyboard.spaceKey.wasPressedThisFrame,
                KeyCode.Return => keyboard.enterKey.wasPressedThisFrame,
                KeyCode.KeypadEnter => keyboard.numpadEnterKey.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetKeyDown(triggerKey);
#endif
        }
    }
}
