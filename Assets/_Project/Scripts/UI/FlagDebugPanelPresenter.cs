using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class FlagDebugPanelPresenter : MonoBehaviour
    {
        private const KeyCode ToggleKey = KeyCode.F3;

        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text contentText;
        [SerializeField] private float refreshIntervalSeconds = 0.25f;
        [SerializeField] private bool showWhenNoFlags = true;
        [SerializeField] private bool startVisible = true;

        private static readonly string[] KnownFlagKeys =
        {
            "debt_pressure",
            "debt_ignored",
            "work_strain",
            "burnout",
            "kyk_risk_days",
            "kyk_focus",
            "kyk_status",
            "illegal_gamble_triggered",
            "illegal_fine_pending",
            "illegal_fine_paid",
            "gamble_stop",
            "gamble_refusal",
            "discipline",
            "family_pressure",
            "guilt",
            "inflation_mode",
            "failed_year_risk",
            "comfort",
            "social_debt",
            "procrastination"
        };

        private IFlagStateService flagStateService;
        private float nextRefreshTime;

        private void Start()
        {
            EnsureUi();
            TryBindService();
            SetPanelVisible(startVisible);
            Refresh();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                SetPanelVisible(!IsPanelVisible());
            }

            if (!IsPanelVisible())
            {
                return;
            }

            if (flagStateService == null)
            {
                TryBindService();
            }

            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + Mathf.Max(0.05f, refreshIntervalSeconds);
            Refresh();
        }

        private void TryBindService()
        {
            if (flagStateService != null)
            {
                return;
            }

            ServiceLocator.TryGet<IFlagStateService>(out flagStateService);
        }

        private void Refresh()
        {
            if (contentText == null)
            {
                return;
            }

            if (flagStateService == null)
            {
                contentText.text = "Flags: service not ready";
                return;
            }

            var sb = new StringBuilder(512);
            sb.AppendLine("Flag Debug");
            var visibleCount = 0;

            for (var i = 0; i < KnownFlagKeys.Length; i++)
            {
                var key = KnownFlagKeys[i];
                if (flagStateService.TryGetText(key, out var textValue))
                {
                    if (!string.IsNullOrWhiteSpace(textValue))
                    {
                        sb.Append(key).Append(": ").Append(textValue).AppendLine();
                        visibleCount++;
                    }

                    continue;
                }

                if (!flagStateService.TryGetNumeric(key, out var numericValue))
                {
                    continue;
                }

                if (Mathf.Abs(numericValue) < 0.001f)
                {
                    continue;
                }

                sb.Append(key).Append(": ").Append(numericValue.ToString("0.##")).AppendLine();
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                if (!showWhenNoFlags)
                {
                    SetPanelVisible(false);

                    return;
                }

                sb.Append("No active flags");
            }

            SetPanelVisible(true);

            contentText.text = sb.ToString().TrimEnd();
        }

        private void EnsureUi()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            if (contentText != null)
            {
                return;
            }

            var existing = transform.Find("FlagDebugText");
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
            {
                contentText = existingText;
                return;
            }

            var go = new GameObject("FlagDebugText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-16f, -16f);
            rect.sizeDelta = new Vector2(360f, 420f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperRight;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = new Color(0.95f, 0.95f, 0.95f, 0.95f);
            text.text = "Flag Debug";

            contentText = text;
        }

        private bool IsPanelVisible()
        {
            return panelRoot == null || panelRoot.activeSelf;
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isVisible);
            }
        }
    }
}
