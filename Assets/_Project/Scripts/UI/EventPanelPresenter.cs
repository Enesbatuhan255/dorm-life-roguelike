using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class EventPanelPresenter : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Text")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text outcomeText;

        [Header("Choices")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceButtonLabels;

        private readonly List<int> visibleChoiceToDataChoice = new List<int>();
        private IEventManager eventManager;
        private EventData activeEventData;
        private bool areEventsSubscribed;
        private bool hasWarnedMissingPanelRoot;

        private void Start()
        {
            SetPanelVisible(false);
            TryGetEventManager(out _);
        }

        private void Update()
        {
            if (eventManager == null)
            {
                TryGetEventManager(out _);
            }
        }

        private void OnDestroy()
        {
            if (eventManager != null && areEventsSubscribed)
            {
                eventManager.OnEventStarted -= HandleEventStarted;
                eventManager.OnEventCompleted -= HandleEventCompleted;
                eventManager.OnOutcomeLogged -= HandleOutcomeLogged;
                areEventsSubscribed = false;
            }
        }

        [ContextMenu("Render Current Event")]
        public void RenderCurrentEvent()
        {
            if (activeEventData == null)
            {
                return;
            }

            if (!TryGetEventManager(out var manager))
            {
                return;
            }

            Render(activeEventData, manager);
        }

        private void Render(EventData eventData, IEventManager manager)
        {
            if (eventData == null)
            {
                SetPanelVisible(false);
                ClearChoices();
                return;
            }

            SetPanelVisible(true);
            SetHeaderText(eventData.Title, eventData.Description);
            BuildVisibleChoices(eventData, manager.GetAvailableChoices(eventData));
            BindChoiceButtons();
        }

        private bool TryGetEventManager(out IEventManager manager)
        {
            if (eventManager != null)
            {
                manager = eventManager;
                return true;
            }

            if (!ServiceLocator.TryGet<IEventManager>(out manager))
            {
                return false;
            }

            eventManager = manager;
            if (!areEventsSubscribed)
            {
                eventManager.OnEventStarted += HandleEventStarted;
                eventManager.OnEventCompleted += HandleEventCompleted;
                eventManager.OnOutcomeLogged += HandleOutcomeLogged;
                areEventsSubscribed = true;
            }

            if (eventManager.CurrentEvent != null)
            {
                activeEventData = eventManager.CurrentEvent;
                Render(activeEventData, eventManager);
            }

            return true;
        }

        private void BuildVisibleChoices(EventData eventData, IReadOnlyList<EventChoice> availableChoices)
        {
            visibleChoiceToDataChoice.Clear();

            for (var i = 0; i < availableChoices.Count; i++)
            {
                var choice = availableChoices[i];
                var index = IndexOfChoice(eventData, choice);
                if (index >= 0)
                {
                    visibleChoiceToDataChoice.Add(index);
                }
            }
        }

        private int IndexOfChoice(EventData eventData, EventChoice target)
        {
            var allChoices = eventData.Choices;
            for (var i = 0; i < allChoices.Count; i++)
            {
                if (ReferenceEquals(allChoices[i], target))
                {
                    return i;
                }
            }

            return -1;
        }

        private void BindChoiceButtons()
        {
            var buttonCount = choiceButtons == null ? 0 : choiceButtons.Length;

            for (var i = 0; i < buttonCount; i++)
            {
                var button = choiceButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();

                var hasChoice = i < visibleChoiceToDataChoice.Count;
                button.gameObject.SetActive(hasChoice);
                button.interactable = hasChoice;

                if (!hasChoice)
                {
                    SetChoiceLabel(i, string.Empty);
                    continue;
                }

                var choiceIndex = visibleChoiceToDataChoice[i];
                SetChoiceLabel(i, BuildChoiceLabel(activeEventData.Choices[choiceIndex]));

                var capturedIndex = i;
                button.onClick.AddListener(() => ApplyVisibleChoice(capturedIndex));
            }
        }

        private void ApplyVisibleChoice(int visibleChoiceIndex)
        {
            if (activeEventData == null)
            {
                return;
            }

            if (!TryGetEventManager(out var manager))
            {
                return;
            }

            if (visibleChoiceIndex < 0 || visibleChoiceIndex >= visibleChoiceToDataChoice.Count)
            {
                SetOutcomeText("Invalid visible choice index.");
                return;
            }

            var dataChoiceIndex = visibleChoiceToDataChoice[visibleChoiceIndex];
            manager.TryApplyChoice(activeEventData, dataChoiceIndex, out var outcome);
            SetOutcomeText(outcome);
        }

        private void ClearChoices()
        {
            if (choiceButtons == null)
            {
                return;
            }

            for (var i = 0; i < choiceButtons.Length; i++)
            {
                var button = choiceButtons[i];
                if (button == null)
                {
                    continue;
                }

                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
                SetChoiceLabel(i, string.Empty);
            }
        }

        private void SetHeaderText(string eventTitle, string eventDescription)
        {
            if (titleText != null)
            {
                titleText.text = eventTitle;
            }

            if (descriptionText != null)
            {
                descriptionText.text = eventDescription;
            }
        }

        private void SetChoiceLabel(int index, string value)
        {
            if (choiceButtonLabels == null || index < 0 || index >= choiceButtonLabels.Length)
            {
                return;
            }

            if (choiceButtonLabels[index] != null)
            {
                choiceButtonLabels[index].text = value;
            }
        }

        private static string BuildChoiceLabel(EventChoice choice)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder(96);
            sb.Append(choice.Text);

            var impacts = BuildImpactPreview(choice);
            if (!string.IsNullOrEmpty(impacts))
            {
                sb.Append('\n').Append(impacts);
            }

            return sb.ToString();
        }

        private static string BuildImpactPreview(EventChoice choice)
        {
            if (choice == null)
            {
                return string.Empty;
            }

            var effects = choice.Effects;
            var parts = new List<string>(6);

            if (effects != null)
            {
                for (var i = 0; i < effects.Count; i++)
                {
                    var effect = effects[i];
                    if (effect == null || Mathf.Approximately(effect.Delta, 0f))
                    {
                        continue;
                    }

                    parts.Add($"{effect.StatType} {FormatSigned(effect.Delta)}");
                }
            }

            if (choice.TimeAdvanceHours > 0)
            {
                parts.Add($"Time +{choice.TimeAdvanceHours}h");
            }

            if (parts.Count == 0)
            {
                return "Etki: belirsiz";
            }

            return string.Join(" | ", parts);
        }

        private static string FormatSigned(float value)
        {
            var abs = Mathf.Abs(value);
            var formatted = abs < 1f ? abs.ToString("0.##") : abs.ToString("0.#");
            return value >= 0f ? "+" + formatted : "-" + formatted;
        }

        private void SetOutcomeText(string value)
        {
            if (outcomeText != null)
            {
                outcomeText.text = value;
            }
        }

        private void SetPanelVisible(bool isVisible)
        {
            if (panelRoot == null)
            {
                if (!hasWarnedMissingPanelRoot)
                {
                    hasWarnedMissingPanelRoot = true;
                    Debug.LogWarning("[EventPanelPresenter] panelRoot is not assigned. Panel visibility will not be controlled.");
                }

                return;
            }

            panelRoot.SetActive(isVisible);
        }

        private void HandleEventStarted(EventData eventData)
        {
            if (!TryGetEventManager(out var manager))
            {
                return;
            }

            activeEventData = eventData;
            Render(activeEventData, manager);
        }

        private void HandleEventCompleted(EventData eventData)
        {
            if (!ReferenceEquals(activeEventData, eventData))
            {
                return;
            }

            activeEventData = null;
            SetPanelVisible(false);
            ClearChoices();
        }

        private void HandleOutcomeLogged(string message)
        {
            SetOutcomeText(message);
        }
    }
}
