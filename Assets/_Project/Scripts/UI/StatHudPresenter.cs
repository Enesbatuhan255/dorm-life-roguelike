using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class StatHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text hungerText;
        [SerializeField] private Text mentalText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text moneyText;
        [SerializeField] private Text academicText;
        [SerializeField] private Text sleepDebtText;

        private IStatSystem statSystem;
        private ISleepDebtSystem sleepDebtSystem;
        private bool isSubscribed;
        private bool isSleepDebtSubscribed;

        private void Start()
        {
            EnsureSleepDebtText();
            TryBindStatSystem();
            TryBindSleepDebtSystem();
        }

        private void Update()
        {
            if (statSystem == null)
            {
                TryBindStatSystem();
            }

            if (sleepDebtSystem == null)
            {
                TryBindSleepDebtSystem();
            }
        }

        private void OnDestroy()
        {
            if (statSystem != null && isSubscribed)
            {
                statSystem.OnStatChanged -= HandleStatChanged;
                isSubscribed = false;
            }

            if (sleepDebtSystem != null && isSleepDebtSubscribed)
            {
                sleepDebtSystem.OnSleepDebtChanged -= HandleSleepDebtChanged;
                isSleepDebtSubscribed = false;
            }
        }

        private void TryBindStatSystem()
        {
            if (!ServiceLocator.TryGet<IStatSystem>(out var resolved))
            {
                return;
            }

            statSystem = resolved;
            if (!isSubscribed)
            {
                statSystem.OnStatChanged += HandleStatChanged;
                isSubscribed = true;
            }

            RefreshAll();
        }

        private void TryBindSleepDebtSystem()
        {
            if (!ServiceLocator.TryGet<ISleepDebtSystem>(out var resolved))
            {
                return;
            }

            sleepDebtSystem = resolved;
            if (!isSleepDebtSubscribed)
            {
                sleepDebtSystem.OnSleepDebtChanged += HandleSleepDebtChanged;
                isSleepDebtSubscribed = true;
            }

            RefreshSleepDebt();
        }

        private void HandleStatChanged(StatChangedEventArgs _)
        {
            RefreshAll();
        }

        private void HandleSleepDebtChanged(float _)
        {
            RefreshSleepDebt();
        }

        private void RefreshAll()
        {
            if (statSystem == null)
            {
                return;
            }

            SetText(hungerText, StatType.Hunger);
            SetText(mentalText, StatType.Mental);
            SetText(energyText, StatType.Energy);
            SetText(moneyText, StatType.Money);
            SetText(academicText, StatType.Academic);
            RefreshSleepDebt();
        }

        private void SetText(Text target, StatType statType)
        {
            if (target == null)
            {
                return;
            }

            var value = statSystem.GetStat(statType);
            target.text = statType + ": " + value.ToString("0.#");
        }

        private void RefreshSleepDebt()
        {
            if (sleepDebtText == null || sleepDebtSystem == null)
            {
                return;
            }

            sleepDebtText.text = "SleepDebt: " + sleepDebtSystem.SleepDebt.ToString("0.#");
        }

        private void EnsureSleepDebtText()
        {
            if (sleepDebtText != null)
            {
                return;
            }

            var existing = transform.Find("SleepDebtText");
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
            {
                sleepDebtText = existingText;
                return;
            }

            var go = new GameObject("SleepDebtText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(320f, 28f);

            var baseY = -178f;
            if (academicText != null)
            {
                var academicRect = academicText.GetComponent<RectTransform>();
                baseY = academicRect.anchoredPosition.y - 32f;
            }

            rect.anchoredPosition = new Vector2(12f, baseY);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = "SleepDebt: 0";

            sleepDebtText = text;
        }
    }
}
