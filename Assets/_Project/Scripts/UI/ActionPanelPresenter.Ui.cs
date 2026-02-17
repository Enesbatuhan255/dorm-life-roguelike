using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed partial class ActionPanelPresenter
    {
        private void EnsureUi()
        {
            if (studyButton == null || sleepButton == null || workButton == null || adminButton == null || waitButton == null || endDayButton == null || socializeButton == null)
            {
                EnsureQuickActionPanel();
            }

            if (planStudyButton == null || planWorkButton == null || planSleepButton == null || planSocializeButton == null
                || planWaitButton == null || planAdminButton == null || runPlanButton == null || clearPlanButton == null)
            {
                EnsurePlannerPanel();
            }

            if (feedbackText == null)
            {
                feedbackText = CreateFeedbackText();
            }

            if (planPreviewText == null)
            {
                planPreviewText = CreatePlanPreviewText();
            }
        }

        private void EnsureQuickActionPanel()
        {
            var existingPanel = transform.Find("ActionPanel");
            if (existingPanel != null)
            {
                studyButton = FindButton(existingPanel, "StudyButton", studyButton);
                sleepButton = FindButton(existingPanel, "SleepButton", sleepButton);
                workButton = FindButton(existingPanel, "WorkButton", workButton);
                adminButton = FindButton(existingPanel, "AdminButton", adminButton);
                socializeButton = FindButton(existingPanel, "SocializeButton", socializeButton);
                waitButton = FindButton(existingPanel, "WaitButton", waitButton);
                endDayButton = FindButton(existingPanel, "EndDayButton", endDayButton);
                feedbackText = FindText(transform, "ActionFeedbackText", feedbackText);
                return;
            }

            var panel = new GameObject(
                "ActionPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(12f, -170f);
            panelRect.sizeDelta = new Vector2(760f, 42f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.35f);

            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(8, 8, 6, 6);

            studyButton = CreateButton(panel.transform, "Study");
            sleepButton = CreateButton(panel.transform, "Sleep");
            workButton = CreateButton(panel.transform, "Work");
            adminButton = CreateButton(panel.transform, "Admin");
            socializeButton = CreateButton(panel.transform, "Socialize");
            waitButton = CreateButton(panel.transform, "Wait");
            endDayButton = CreateButton(panel.transform, "End Day");
        }

        private void EnsurePlannerPanel()
        {
            var existingPanel = transform.Find("PlannerPanel");
            if (existingPanel != null)
            {
                planStudyButton = FindButton(existingPanel, "PlanStudyButton", planStudyButton);
                planWorkButton = FindButton(existingPanel, "PlanWorkButton", planWorkButton);
                planSleepButton = FindButton(existingPanel, "PlanSleepButton", planSleepButton);
                planSocializeButton = FindButton(existingPanel, "PlanSocializeButton", planSocializeButton);
                planWaitButton = FindButton(existingPanel, "PlanWaitButton", planWaitButton);
                planAdminButton = FindButton(existingPanel, "PlanAdminButton", planAdminButton);
                runPlanButton = FindButton(existingPanel, "RunPlanButton", runPlanButton);
                clearPlanButton = FindButton(existingPanel, "ClearPlanButton", clearPlanButton);
                return;
            }

            var panel = new GameObject(
                "PlannerPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(12f, -220f);
            panelRect.sizeDelta = new Vector2(980f, 42f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.35f);

            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(8, 8, 6, 6);

            planStudyButton = CreateButton(panel.transform, "Plan Study");
            planWorkButton = CreateButton(panel.transform, "Plan Work");
            planSleepButton = CreateButton(panel.transform, "Plan Sleep");
            planSocializeButton = CreateButton(panel.transform, "Plan Social");
            planWaitButton = CreateButton(panel.transform, "Plan Wait");
            planAdminButton = CreateButton(panel.transform, "Plan Admin");
            runPlanButton = CreateButton(panel.transform, "Run Plan");
            clearPlanButton = CreateButton(panel.transform, "Clear Plan");
        }

        private Text CreateFeedbackText()
        {
            var feedbackObject = new GameObject("ActionFeedbackText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            feedbackObject.transform.SetParent(transform, false);

            var rect = feedbackObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -268f);
            rect.sizeDelta = new Vector2(980f, 26f);

            var text = feedbackObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 20;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.text = string.Empty;
            return text;
        }

        private Text CreatePlanPreviewText()
        {
            var previewObject = new GameObject("PlanPreviewText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            previewObject.transform.SetParent(transform, false);

            var rect = previewObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(12f, -298f);
            rect.sizeDelta = new Vector2(980f, 54f);

            var text = previewObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = new Color(0.9f, 0.95f, 1f, 1f);
            text.text = "Plan: -";
            return text;
        }

        private static Button CreateButton(Transform parent, string title)
        {
            var objectName = title.Replace(" ", string.Empty) + "Button";
            var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.17f, 0.25f, 0.38f, 0.95f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 16;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = title;

            return button;
        }

        private static Button FindButton(Transform root, string childName, Button fallback)
        {
            if (fallback != null)
            {
                return fallback;
            }

            var child = root.Find(childName);
            return child != null ? child.GetComponent<Button>() : null;
        }

        private static Text FindText(Transform root, string childName, Text fallback)
        {
            if (fallback != null)
            {
                return fallback;
            }

            var child = root.Find(childName);
            return child != null ? child.GetComponent<Text>() : null;
        }
    }
}
