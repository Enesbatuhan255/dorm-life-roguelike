using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class DayChangePopupPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private Text popupText;
        [SerializeField] private float visibleSeconds = 2.5f;

        private ITimeManager timeManager;
        private IEconomySystem economySystem;
        private bool isTimeSubscribed;
        private bool isEconomySubscribed;
        private int lastCostDay = -1;
        private float lastCostTotal;
        private Coroutine hideRoutine;

        private void Start()
        {
            EnsurePopupUi();
            SetPopupVisible(false);
            TryBindServices();
        }

        private void Update()
        {
            if (timeManager == null || economySystem == null)
            {
                TryBindServices();
            }
        }

        private void OnDestroy()
        {
            if (timeManager != null && isTimeSubscribed)
            {
                timeManager.OnDayChanged -= HandleDayChanged;
                isTimeSubscribed = false;
            }

            if (economySystem != null && isEconomySubscribed)
            {
                economySystem.OnTransactionApplied -= HandleTransactionApplied;
                isEconomySubscribed = false;
            }
        }

        private void TryBindServices()
        {
            if (timeManager == null && ServiceLocator.TryGet<ITimeManager>(out var resolvedTime))
            {
                timeManager = resolvedTime;
                if (!isTimeSubscribed)
                {
                    timeManager.OnDayChanged += HandleDayChanged;
                    isTimeSubscribed = true;
                }
            }

            if (economySystem == null && ServiceLocator.TryGet<IEconomySystem>(out var resolvedEconomy))
            {
                economySystem = resolvedEconomy;
                if (!isEconomySubscribed)
                {
                    economySystem.OnTransactionApplied += HandleTransactionApplied;
                    isEconomySubscribed = true;
                }
            }
        }

        private void HandleDayChanged(int newDay)
        {
            var summary = "Gunluk giderler uygulandi";
            if (lastCostDay == newDay && lastCostTotal < 0f)
            {
                summary += $" ({lastCostTotal:0.#})";
            }

            ShowPopup($"Yeni Gun: D{newDay}\n{summary}");
        }

        private void HandleTransactionApplied(float amount, string reason)
        {
            if (timeManager == null || amount >= 0f || !IsDailyCostReason(reason))
            {
                return;
            }

            var currentDay = timeManager.Day;
            if (lastCostDay != currentDay)
            {
                lastCostDay = currentDay;
                lastCostTotal = 0f;
            }

            lastCostTotal += amount;
        }

        private static bool IsDailyCostReason(string reason)
        {
            return reason == "Food"
                || reason == "Transport"
                || reason == "Unexpected expense";
        }

        private void ShowPopup(string message)
        {
            if (popupText != null)
            {
                popupText.text = message;
            }

            SetPopupVisible(true);

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSeconds(Mathf.Max(0.5f, visibleSeconds));
            SetPopupVisible(false);
            hideRoutine = null;
        }

        private void SetPopupVisible(bool isVisible)
        {
            if (popupRoot != null)
            {
                popupRoot.SetActive(isVisible);
            }
        }

        private void EnsurePopupUi()
        {
            if (popupRoot != null && popupText != null)
            {
                return;
            }

            var existingRoot = transform.Find("DayChangePopup");
            if (existingRoot != null)
            {
                popupRoot = existingRoot.gameObject;
                if (popupText == null)
                {
                    popupText = existingRoot.GetComponentInChildren<Text>(true);
                }

                return;
            }

            var root = new GameObject(
                "DayChangePopup",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            root.transform.SetParent(transform, false);

            var rootRect = root.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0f, -20f);
            rootRect.sizeDelta = new Vector2(520f, 80f);

            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0.72f);

            var textObject = new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            textObject.transform.SetParent(root.transform, false);

            var textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 10f);
            textRect.offsetMax = new Vector2(-12f, -10f);

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = "Yeni Gun: D1";

            popupRoot = root;
            popupText = text;
        }
    }
}
