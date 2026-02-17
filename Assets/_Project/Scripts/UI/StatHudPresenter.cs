using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class StatHudPresenter : MonoBehaviour
    {
        [Header("HUD Style")]
        [SerializeField] private bool useRichText = true;
        [SerializeField] private Color goodColor = new Color(0.58f, 1f, 0.62f, 1f);
        [SerializeField] private Color warnColor = new Color(1f, 0.86f, 0.45f, 1f);
        [SerializeField] private Color dangerColor = new Color(1f, 0.58f, 0.58f, 1f);
        [SerializeField] private Color neutralColor = Color.white;

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
            target.supportRichText = useRichText;
            target.text = BuildStatLine(statType, value);
        }

        private void RefreshSleepDebt()
        {
            if (sleepDebtText == null || sleepDebtSystem == null)
            {
                return;
            }

            sleepDebtText.supportRichText = useRichText;
            var debt = sleepDebtSystem.SleepDebt;
            var riskTag = debt >= 8f ? "HIGH" : debt >= 4f ? "MID" : "LOW";
            var color = debt >= 8f ? dangerColor : debt >= 4f ? warnColor : goodColor;
            sleepDebtText.text = BuildLine("Sleep Debt", debt, riskTag, color, "0.#");
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

        private string BuildStatLine(StatType statType, float value)
        {
            switch (statType)
            {
                case StatType.Hunger:
                {
                    var riskTag = value < 25f ? "CRITICAL" : value < 45f ? "LOW" : "OK";
                    var color = value < 25f ? dangerColor : value < 45f ? warnColor : goodColor;
                    return BuildLine("Hunger", value, riskTag, color, "0.#");
                }
                case StatType.Mental:
                {
                    var riskTag = value < 30f ? "CRITICAL" : value < 55f ? "LOW" : "OK";
                    var color = value < 30f ? dangerColor : value < 55f ? warnColor : goodColor;
                    return BuildLine("Mental", value, riskTag, color, "0.#");
                }
                case StatType.Energy:
                {
                    var riskTag = value < 20f ? "CRITICAL" : value < 45f ? "LOW" : "OK";
                    var color = value < 20f ? dangerColor : value < 45f ? warnColor : goodColor;
                    return BuildLine("Energy", value, riskTag, color, "0.#");
                }
                case StatType.Money:
                {
                    var riskTag = value < -1000f ? "DEBT+" : value < -200f ? "DEBT" : value < 200f ? "TIGHT" : "SAFE";
                    var color = value < -1000f ? dangerColor : value < -200f ? warnColor : value < 200f ? neutralColor : goodColor;
                    return BuildLine("Money", value, riskTag, color, "0");
                }
                case StatType.Academic:
                {
                    var riskTag = value < 1.8f ? "RISK" : value < 2.4f ? "MID" : "GOOD";
                    var color = value < 1.8f ? dangerColor : value < 2.4f ? warnColor : goodColor;
                    return BuildLine("Academic", value, riskTag, color, "0.00");
                }
                default:
                    return BuildLine(statType.ToString(), value, "-", neutralColor, "0.#");
            }
        }

        private string BuildLine(string label, float value, string tag, Color tagColor, string valueFormat)
        {
            if (!useRichText)
            {
                return $"{label}: {value.ToString(valueFormat)} [{tag}]";
            }

            return $"{label}: {value.ToString(valueFormat)} {Colorize($"[{tag}]", tagColor)}";
        }

        private static string Colorize(string text, Color color)
        {
            var hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{text}</color>";
        }
    }
}
