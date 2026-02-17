using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed partial class MicroChallengePanelPresenter
    {
        private void EnsureStatusText()
        {
            if (statusText != null)
            {
                return;
            }

            var existing = transform.Find("MicroChallengeStatusText");
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
            {
                statusText = existingText;
                return;
            }

            var go = new GameObject("MicroChallengeStatusText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -360f);
            rect.sizeDelta = new Vector2(1100f, 48f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(1f, 0.92f, 0.72f, 1f);
            text.text = "Challenge: -";

            statusText = text;
        }

        private void EnsureInteractiveUi()
        {
            if (interactiveRoot != null
                && interactiveTitleText != null
                && interactiveTimerText != null
                && interactiveHitText != null
                && interactiveHitButton != null
                && interactiveAdminOptionAButton != null
                && interactiveAdminOptionBButton != null
                && interactiveAdminOptionCButton != null)
            {
                return;
            }

            var rootTransform = transform.Find("MicroChallengeInteractiveRoot");
            if (rootTransform != null)
            {
                interactiveRoot = rootTransform.gameObject;
                interactiveTitleText = rootTransform.Find("TitleText")?.GetComponent<Text>();
                interactiveTimerText = rootTransform.Find("TimerText")?.GetComponent<Text>();
                interactiveHitText = rootTransform.Find("HitText")?.GetComponent<Text>();
                interactiveHitButton = rootTransform.Find("HitButton")?.GetComponent<Button>();
                interactiveAdminOptionAButton = rootTransform.Find("AdminOptionAButton")?.GetComponent<Button>();
                interactiveAdminOptionBButton = rootTransform.Find("AdminOptionBButton")?.GetComponent<Button>();
                interactiveAdminOptionCButton = rootTransform.Find("AdminOptionCButton")?.GetComponent<Button>();
                BindInteractiveButtons();
                return;
            }

            interactiveRoot = new GameObject("MicroChallengeInteractiveRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            interactiveRoot.transform.SetParent(transform, false);
            var rootRect = interactiveRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = new Vector2(12f, -415f);
            rootRect.sizeDelta = new Vector2(520f, 120f);
            interactiveRoot.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

            interactiveTitleText = CreateInteractiveText(interactiveRoot.transform, "TitleText", new Vector2(10f, -10f), new Vector2(500f, 24f), TextAnchor.MiddleLeft, 16);
            interactiveTimerText = CreateInteractiveText(interactiveRoot.transform, "TimerText", new Vector2(10f, -38f), new Vector2(220f, 22f), TextAnchor.MiddleLeft, 15);
            interactiveHitText = CreateInteractiveText(interactiveRoot.transform, "HitText", new Vector2(240f, -38f), new Vector2(260f, 22f), TextAnchor.MiddleLeft, 15);
            interactiveInfoText = CreateInteractiveText(interactiveRoot.transform, "InfoText", new Vector2(10f, -96f), new Vector2(480f, 20f), TextAnchor.MiddleLeft, 14);
            interactiveHitButton = CreateInteractiveButton(interactiveRoot.transform, "HitButton", "Hit!", new Vector2(10f, -68f), new Vector2(140f, 38f));
            interactiveAdminOptionAButton = CreateInteractiveButton(interactiveRoot.transform, "AdminOptionAButton", "1", new Vector2(10f, -68f), new Vector2(100f, 38f));
            interactiveAdminOptionBButton = CreateInteractiveButton(interactiveRoot.transform, "AdminOptionBButton", "2", new Vector2(120f, -68f), new Vector2(100f, 38f));
            interactiveAdminOptionCButton = CreateInteractiveButton(interactiveRoot.transform, "AdminOptionCButton", "3", new Vector2(230f, -68f), new Vector2(100f, 38f));

            BindInteractiveButtons();
            SetStudyUiVisible(true);
            SetAdminUiVisible(false);
            interactiveRoot.SetActive(false);
        }

        private void BindInteractiveButtons()
        {
            if (interactiveHitButton != null)
            {
                interactiveHitButton.onClick.RemoveListener(HandleInteractiveHit);
                interactiveHitButton.onClick.AddListener(HandleInteractiveHit);
            }

            if (interactiveAdminOptionAButton != null)
            {
                interactiveAdminOptionAButton.onClick.RemoveListener(HandleAdminOptionA);
                interactiveAdminOptionAButton.onClick.AddListener(HandleAdminOptionA);
            }

            if (interactiveAdminOptionBButton != null)
            {
                interactiveAdminOptionBButton.onClick.RemoveListener(HandleAdminOptionB);
                interactiveAdminOptionBButton.onClick.AddListener(HandleAdminOptionB);
            }

            if (interactiveAdminOptionCButton != null)
            {
                interactiveAdminOptionCButton.onClick.RemoveListener(HandleAdminOptionC);
                interactiveAdminOptionCButton.onClick.AddListener(HandleAdminOptionC);
            }
        }

        private static Text CreateInteractiveText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, TextAnchor anchor, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private static Button CreateInteractiveButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = new Color(0.17f, 0.25f, 0.38f, 0.95f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGo.GetComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            labelText.text = label;
            return button;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null)
            {
                return;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private void SetInfoText(string message)
        {
            if (interactiveInfoText != null)
            {
                interactiveInfoText.text = message;
            }
        }

        private void SetInfoColor(Color color)
        {
            if (interactiveInfoText != null)
            {
                interactiveInfoText.color = color;
            }
        }

        private static Color GetBandColor(MicroChallengeOutcomeBand band)
        {
            switch (band)
            {
                case MicroChallengeOutcomeBand.Perfect:
                    return new Color(0.55f, 1f, 0.55f, 1f);
                case MicroChallengeOutcomeBand.Poor:
                    return new Color(1f, 0.60f, 0.60f, 1f);
                default:
                    return new Color(1f, 0.92f, 0.72f, 1f);
            }
        }
    }
}
