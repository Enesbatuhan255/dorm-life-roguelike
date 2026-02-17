using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class SaveLoadPanelPresenter : MonoBehaviour
    {
        [Header("Optional UI References")]
        [SerializeField] private Button quickSaveButton;
        [SerializeField] private Button quickLoadButton;
        [SerializeField] private Button slot1SaveButton;
        [SerializeField] private Button slot1LoadButton;
        [SerializeField] private Button slot2SaveButton;
        [SerializeField] private Button slot2LoadButton;
        [SerializeField] private Button slot3SaveButton;
        [SerializeField] private Button slot3LoadButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text summaryText;

        [Header("Refresh")]
        [SerializeField] private float summaryRefreshSeconds = 1f;
        [SerializeField] private float horizontalPadding = 12f;

        private ISaveLoadService saveLoadService;
        private bool listenersBound;
        private float nextRefreshAt;
        private int lastScreenWidth = -1;
        private int lastScreenHeight = -1;

        private void Start()
        {
            EnsureUi();
            TryBindService();
            BindListeners();
            ApplyButtonVisuals();
            RefreshResponsiveLayout(force: true);
            RefreshSummary();
            SetStatus("Save/Load hazir");
        }

        private void Update()
        {
            if (saveLoadService == null)
            {
                TryBindService();
            }

            if (Time.unscaledTime < nextRefreshAt)
            {
                return;
            }

            nextRefreshAt = Time.unscaledTime + Mathf.Max(0.25f, summaryRefreshSeconds);
            RefreshResponsiveLayout(force: false);
            RefreshSummary();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        private void EnsureUi()
        {
            var existingPanel = transform.Find("SaveLoadPanel");
            if (existingPanel != null)
            {
                quickSaveButton = FindButton(existingPanel, "QuickSaveButton", quickSaveButton);
                quickLoadButton = FindButton(existingPanel, "QuickLoadButton", quickLoadButton);
                slot1SaveButton = FindButton(existingPanel, "S1SaveButton", slot1SaveButton);
                slot1LoadButton = FindButton(existingPanel, "S1LoadButton", slot1LoadButton);
                slot2SaveButton = FindButton(existingPanel, "S2SaveButton", slot2SaveButton);
                slot2LoadButton = FindButton(existingPanel, "S2LoadButton", slot2LoadButton);
                slot3SaveButton = FindButton(existingPanel, "S3SaveButton", slot3SaveButton);
                slot3LoadButton = FindButton(existingPanel, "S3LoadButton", slot3LoadButton);
            }
            else
            {
                BuildRuntimePanel();
            }

            if (statusText == null)
            {
                statusText = CreateText("SaveLoadStatusText", -366f, 24f, 16, TextAnchor.MiddleLeft, Color.white);
            }

            if (summaryText == null)
            {
                summaryText = CreateText("SaveLoadSummaryText", -394f, 84f, 15, TextAnchor.UpperLeft, new Color(0.9f, 0.95f, 1f, 1f));
            }

            if (summaryText != null)
            {
                summaryText.supportRichText = true;
            }
        }

        private void BuildRuntimePanel()
        {
            var panel = new GameObject(
                "SaveLoadPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(HorizontalLayoutGroup));
            panel.transform.SetParent(transform, false);

            var panelRect = panel.GetComponent<RectTransform>();
            ConfigureTopStretchRect(panelRect, topY: -330f, height: 30f);

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.35f);

            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 6f;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.padding = new RectOffset(8, 8, 4, 4);

            quickSaveButton = CreateButton(panel.transform, "Quick Save");
            quickLoadButton = CreateButton(panel.transform, "Quick Load");
            slot1SaveButton = CreateButton(panel.transform, "S1 Save");
            slot1LoadButton = CreateButton(panel.transform, "S1 Load");
            slot2SaveButton = CreateButton(panel.transform, "S2 Save");
            slot2LoadButton = CreateButton(panel.transform, "S2 Load");
            slot3SaveButton = CreateButton(panel.transform, "S3 Save");
            slot3LoadButton = CreateButton(panel.transform, "S3 Load");
        }

        private void TryBindService()
        {
            if (saveLoadService != null)
            {
                return;
            }

            ServiceLocator.TryGet<ISaveLoadService>(out saveLoadService);
        }

        private void BindListeners()
        {
            if (listenersBound)
            {
                return;
            }

            quickSaveButton?.onClick.AddListener(HandleQuickSave);
            quickLoadButton?.onClick.AddListener(HandleQuickLoad);
            slot1SaveButton?.onClick.AddListener(() => HandleSaveSlot(SaveLoadService.Slot1));
            slot1LoadButton?.onClick.AddListener(() => HandleLoadSlot(SaveLoadService.Slot1));
            slot2SaveButton?.onClick.AddListener(() => HandleSaveSlot(SaveLoadService.Slot2));
            slot2LoadButton?.onClick.AddListener(() => HandleLoadSlot(SaveLoadService.Slot2));
            slot3SaveButton?.onClick.AddListener(() => HandleSaveSlot(SaveLoadService.Slot3));
            slot3LoadButton?.onClick.AddListener(() => HandleLoadSlot(SaveLoadService.Slot3));
            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            quickSaveButton?.onClick.RemoveListener(HandleQuickSave);
            quickLoadButton?.onClick.RemoveListener(HandleQuickLoad);
            slot1SaveButton?.onClick.RemoveAllListeners();
            slot1LoadButton?.onClick.RemoveAllListeners();
            slot2SaveButton?.onClick.RemoveAllListeners();
            slot2LoadButton?.onClick.RemoveAllListeners();
            slot3SaveButton?.onClick.RemoveAllListeners();
            slot3LoadButton?.onClick.RemoveAllListeners();
            listenersBound = false;
        }

        private void HandleQuickSave()
        {
            if (!CanUseService())
            {
                return;
            }

            try
            {
                saveLoadService.SaveQuick();
                SetStatus("Quick Save tamam");
                RefreshSummary();
            }
            catch (Exception e)
            {
                SetStatus("Quick Save hata: " + e.Message);
            }
        }

        private void HandleQuickLoad()
        {
            if (!CanUseService())
            {
                return;
            }

            try
            {
                var loaded = saveLoadService.LoadQuick();
                SetStatus(loaded ? "Quick Load tamam" : "Quick Load basarisiz (kayit yok)");
                RefreshSummary();
            }
            catch (Exception e)
            {
                SetStatus("Quick Load hata: " + e.Message);
            }
        }

        private void HandleSaveSlot(string slotId)
        {
            if (!CanUseService())
            {
                return;
            }

            try
            {
                saveLoadService.SaveToSlot(slotId);
                SetStatus(slotId + " kaydedildi");
                RefreshSummary();
            }
            catch (Exception e)
            {
                SetStatus(slotId + " save hata: " + e.Message);
            }
        }

        private void HandleLoadSlot(string slotId)
        {
            if (!CanUseService())
            {
                return;
            }

            try
            {
                var loaded = saveLoadService.LoadFromSlot(slotId);
                SetStatus(loaded ? slotId + " yuklendi" : slotId + " icin kayit yok");
                RefreshSummary();
            }
            catch (Exception e)
            {
                SetStatus(slotId + " load hata: " + e.Message);
            }
        }

        private bool CanUseService()
        {
            if (saveLoadService != null)
            {
                return true;
            }

            TryBindService();
            if (saveLoadService != null)
            {
                return true;
            }

            SetStatus("Save/Load servisi hazir degil");
            return false;
        }

        private void RefreshSummary()
        {
            if (summaryText == null)
            {
                return;
            }

            if (saveLoadService == null)
            {
                summaryText.text = "Slot ozetleri: servis bekleniyor";
                return;
            }

            var summaries = saveLoadService.GetSlotSummaries();
            summaryText.text = BuildSummaryText(summaries);
        }

        private static string BuildSummaryText(IReadOnlyList<SaveSlotSummary> summaries)
        {
            if (summaries == null || summaries.Count == 0)
            {
                return "Slot ozeti yok";
            }

            var sb = new StringBuilder(256);
            sb.AppendLine("<b>Slot Ozetleri</b>");
            for (var i = 0; i < summaries.Count; i++)
            {
                var item = summaries[i];
                if (item == null)
                {
                    continue;
                }

                if (!item.Exists)
                {
                    sb.Append("- ").Append(item.SlotId).Append(": bos").AppendLine();
                    continue;
                }

                var stamp = FormatUtcStamp(item.SavedAtUtc);
                sb.Append("- ")
                    .Append(item.SlotId)
                    .Append(": Day ")
                    .Append(item.Day)
                    .Append(" ")
                    .Append(item.Hour.ToString("00"))
                    .Append(":00 | ")
                    .Append(stamp)
                    .AppendLine();
            }

            return sb.ToString().TrimEnd();
        }

        private static string FormatUtcStamp(string savedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(savedAtUtc))
            {
                return "-";
            }

            if (!DateTime.TryParse(savedAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsedUtc))
            {
                return savedAtUtc;
            }

            var local = parsedUtc.Kind == DateTimeKind.Utc ? parsedUtc.ToLocalTime() : parsedUtc;
            return local.ToString("dd.MM HH:mm", CultureInfo.InvariantCulture);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }

        private void ApplyButtonVisuals()
        {
            StyleButton(quickSaveButton, isSaveAction: true, isQuickAction: true);
            StyleButton(quickLoadButton, isSaveAction: false, isQuickAction: true);
            StyleButton(slot1SaveButton, isSaveAction: true, isQuickAction: false);
            StyleButton(slot1LoadButton, isSaveAction: false, isQuickAction: false);
            StyleButton(slot2SaveButton, isSaveAction: true, isQuickAction: false);
            StyleButton(slot2LoadButton, isSaveAction: false, isQuickAction: false);
            StyleButton(slot3SaveButton, isSaveAction: true, isQuickAction: false);
            StyleButton(slot3LoadButton, isSaveAction: false, isQuickAction: false);
        }

        private static void StyleButton(Button button, bool isSaveAction, bool isQuickAction)
        {
            if (button == null || !button.TryGetComponent<Image>(out var image))
            {
                return;
            }

            var baseColor = isSaveAction
                ? new Color(0.13f, 0.36f, 0.26f, 0.95f)
                : new Color(0.20f, 0.24f, 0.40f, 0.95f);

            if (isQuickAction)
            {
                baseColor += new Color(0.07f, 0.07f, 0.07f, 0f);
            }

            image.color = baseColor;
        }

        private Text CreateText(string objectName, float topY, float height, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            ConfigureTopStretchRect(rect, topY, height);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = color;
            text.text = string.Empty;
            return text;
        }

        private void RefreshResponsiveLayout(bool force)
        {
            if (!force && lastScreenWidth == Screen.width && lastScreenHeight == Screen.height)
            {
                return;
            }

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;

            var panel = transform.Find("SaveLoadPanel");
            if (panel != null && panel.TryGetComponent<RectTransform>(out var panelRect))
            {
                ConfigureTopStretchRect(panelRect, topY: -330f, height: 30f);
            }

            if (statusText != null)
            {
                ConfigureTopStretchRect(statusText.GetComponent<RectTransform>(), topY: -366f, height: 24f);
            }

            if (summaryText != null)
            {
                ConfigureTopStretchRect(summaryText.GetComponent<RectTransform>(), topY: -394f, height: 84f);
            }
        }

        private void ConfigureTopStretchRect(RectTransform rect, float topY, float height)
        {
            if (rect == null)
            {
                return;
            }

            var pad = Mathf.Max(0f, horizontalPadding);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(pad, topY - height);
            rect.offsetMax = new Vector2(-pad, topY);
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
            label.fontSize = 14;
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
    }
}
