using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DormLifeRoguelike
{
    public sealed class TimeDebugAdvanceInput : MonoBehaviour
    {
        [SerializeField] private KeyCode advanceKey = KeyCode.T;
        [SerializeField] private int hoursPerPress = 1;

        private void Update()
        {
            if (!IsAdvancePressed())
            {
                return;
            }

            if (!ServiceLocator.TryGet<ITimeManager>(out var timeManager))
            {
                return;
            }

            var hours = Mathf.Max(1, hoursPerPress);
            timeManager.AdvanceTime(hours);
        }

        private bool IsAdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return false;
            }

            return advanceKey switch
            {
                KeyCode.T => keyboard.tKey.wasPressedThisFrame,
                KeyCode.Y => keyboard.yKey.wasPressedThisFrame,
                KeyCode.U => keyboard.uKey.wasPressedThisFrame,
                _ => false
            };
#else
            return Input.GetKeyDown(advanceKey);
#endif
        }
    }
}
