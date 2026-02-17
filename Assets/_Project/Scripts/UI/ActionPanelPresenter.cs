using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class ActionPanelPresenter : MonoBehaviour
    {
        [Header("Optional UI References")]
        [SerializeField] private Button studyButton;
        [SerializeField] private Button sleepButton;
        [SerializeField] private Button workButton;
        [SerializeField] private Button adminButton;
        [SerializeField] private Button socializeButton;
        [SerializeField] private Button waitButton;
        [SerializeField] private Button endDayButton;
        [SerializeField] private Text feedbackText;

        [Header("Planner UI")]
        [SerializeField] private Button planStudyButton;
        [SerializeField] private Button planWorkButton;
        [SerializeField] private Button planSleepButton;
        [SerializeField] private Button planSocializeButton;
        [SerializeField] private Button planWaitButton;
        [SerializeField] private Button planAdminButton;
        [SerializeField] private Button runPlanButton;
        [SerializeField] private Button clearPlanButton;
        [SerializeField] private Text planPreviewText;

        [Header("Action Tuning")]
        [SerializeField] private int studyHours = 2;
        [SerializeField] private int sleepHours = 8;
        [SerializeField] private int workHours = 4;
        [SerializeField] private int adminHours = 2;
        [SerializeField] private int socializeHours = 1;
        [SerializeField] private int waitHours = 1;

        private IPlayerActionService playerActionService;
        private IDayPlanningService dayPlanningService;
        private ITimeManager timeManager;
        private IGameOutcomeSystem gameOutcomeSystem;
        private IEventManager eventManager;
        private IMicroChallengeService microChallengeService;
        private IMicroChallengeInteractiveRunner microChallengeRunner;
        private bool listenersBound;
        private bool isOutcomeSubscribed;
        private bool isInteractiveChallengePending;

        private void Start()
        {
            EnsureUi();
            TryBindServices();
            BindButtonListeners();
            RefreshActionAvailability();
            RefreshFeedback();
            RefreshPlanPreview();
        }

        private void Update()
        {
            if (playerActionService == null || timeManager == null || gameOutcomeSystem == null || dayPlanningService == null)
            {
                TryBindServices();
            }

            RefreshActionAvailability();
        }

        private void OnDestroy()
        {
            if (gameOutcomeSystem != null && isOutcomeSubscribed)
            {
                gameOutcomeSystem.OnGameEnded -= HandleOutcomeResolved;
                isOutcomeSubscribed = false;
            }

            UnbindButtonListeners();
        }

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

        private void BindButtonListeners()
        {
            if (listenersBound)
            {
                return;
            }

            studyButton?.onClick.AddListener(HandleStudyClicked);
            sleepButton?.onClick.AddListener(HandleSleepClicked);
            workButton?.onClick.AddListener(HandleWorkClicked);
            adminButton?.onClick.AddListener(HandleAdminClicked);
            socializeButton?.onClick.AddListener(HandleSocializeClicked);
            waitButton?.onClick.AddListener(HandleWaitClicked);
            endDayButton?.onClick.AddListener(HandleEndDayClicked);

            planStudyButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Study));
            planWorkButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Work));
            planSleepButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Sleep));
            planSocializeButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Socialize));
            planWaitButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Wait));
            planAdminButton?.onClick.AddListener(() => HandlePlanAdd(PlannedActionType.Admin));
            runPlanButton?.onClick.AddListener(HandleRunPlanClicked);
            clearPlanButton?.onClick.AddListener(HandleClearPlanClicked);

            listenersBound = true;
        }

        private void UnbindButtonListeners()
        {
            if (!listenersBound)
            {
                return;
            }

            studyButton?.onClick.RemoveListener(HandleStudyClicked);
            sleepButton?.onClick.RemoveListener(HandleSleepClicked);
            workButton?.onClick.RemoveListener(HandleWorkClicked);
            adminButton?.onClick.RemoveListener(HandleAdminClicked);
            socializeButton?.onClick.RemoveListener(HandleSocializeClicked);
            waitButton?.onClick.RemoveListener(HandleWaitClicked);
            endDayButton?.onClick.RemoveListener(HandleEndDayClicked);

            planStudyButton?.onClick.RemoveAllListeners();
            planWorkButton?.onClick.RemoveAllListeners();
            planSleepButton?.onClick.RemoveAllListeners();
            planSocializeButton?.onClick.RemoveAllListeners();
            planWaitButton?.onClick.RemoveAllListeners();
            planAdminButton?.onClick.RemoveAllListeners();
            runPlanButton?.onClick.RemoveListener(HandleRunPlanClicked);
            clearPlanButton?.onClick.RemoveListener(HandleClearPlanClicked);

            listenersBound = false;
        }

        private void HandlePlanAdd(PlannedActionType actionType)
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            if (!dayPlanningService.TryAddBlock(actionType))
            {
                RefreshFeedback("Plan is full (8 blocks)");
                return;
            }

            RefreshPlanPreview();
            RefreshFeedback($"Planned: {actionType}");
        }

        private void HandleRunPlanClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            var result = dayPlanningService.ExecutePlan();
            var notes = result.Notes.Count > 0 ? $" ({result.Notes[0]})" : string.Empty;
            RefreshPlanPreview();
            RefreshFeedback($"Plan run: {result.ExecutedBlocks} ok, {result.RejectedBlocks} blocked{notes}");
        }

        private void HandleClearPlanClicked()
        {
            if (!TryBindServices())
            {
                return;
            }

            dayPlanningService.ClearPlan();
            RefreshPlanPreview();
            RefreshFeedback("Plan cleared");
        }

        private void HandleStudyClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            if (isInteractiveChallengePending)
            {
                RefreshFeedback("Study challenge already running");
                return;
            }

            if (TryRunInteractiveChallenge(PlannedActionType.Study, studyHours))
            {
                RefreshFeedback("Study challenge started");
                return;
            }

            playerActionService.ApplyStudy(studyHours, ResolveOutcomeBandForAction(PlannedActionType.Study));
            RefreshFeedback("Study");
        }

        private void HandleSleepClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            playerActionService.ApplySleep(sleepHours);
            RefreshFeedback("Sleep");
        }

        private void HandleWorkClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            if (!playerActionService.CanWorkThisWeek)
            {
                RefreshFeedback("Work limit reached");
                return;
            }

            if (isInteractiveChallengePending)
            {
                RefreshFeedback("Challenge already running");
                return;
            }

            if (TryRunInteractiveChallenge(PlannedActionType.Work, workHours))
            {
                RefreshFeedback("Work challenge started");
                return;
            }

            playerActionService.ApplyWork(workHours, ResolveOutcomeBandForAction(PlannedActionType.Work));
            RefreshFeedback("Work");
        }

        private void HandleAdminClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            if (isInteractiveChallengePending)
            {
                RefreshFeedback("Challenge already running");
                return;
            }

            if (TryRunInteractiveChallenge(PlannedActionType.Admin, adminHours))
            {
                RefreshFeedback("Admin challenge started");
                return;
            }

            playerActionService.ApplyAdmin(adminHours, ResolveOutcomeBandForAction(PlannedActionType.Admin));
            RefreshFeedback("Admin");
        }

        private void HandleSocializeClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            playerActionService.ApplySocialize(socializeHours);
            RefreshFeedback("Socialize");
        }

        private void HandleWaitClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            playerActionService.ApplyWait(waitHours);
            RefreshFeedback("Wait");
        }

        private void HandleEndDayClicked()
        {
            if (!TryBindServices() || IsGameResolved())
            {
                RefreshFeedback();
                return;
            }

            if (playerActionService.TryEndDay(out var message))
            {
                RefreshFeedback(message);
                return;
            }

            RefreshFeedback(message);
        }

        private bool TryBindServices()
        {
            if (ServiceLocator.TryGet<IPlayerActionService>(out var resolvedActionService))
            {
                playerActionService = resolvedActionService;
            }

            if (ServiceLocator.TryGet<IDayPlanningService>(out var resolvedPlanningService))
            {
                dayPlanningService = resolvedPlanningService;
            }

            if (ServiceLocator.TryGet<ITimeManager>(out var resolvedTimeManager))
            {
                timeManager = resolvedTimeManager;
            }

            if (eventManager == null && ServiceLocator.TryGet<IEventManager>(out var resolvedEvents))
            {
                eventManager = resolvedEvents;
            }

            if (microChallengeService == null && ServiceLocator.TryGet<IMicroChallengeService>(out var resolvedChallenges))
            {
                microChallengeService = resolvedChallenges;
            }

            if (microChallengeRunner == null)
            {
                microChallengeRunner = FindFirstObjectByType<MicroChallengePanelPresenter>();
            }

            if (gameOutcomeSystem == null && ServiceLocator.TryGet<IGameOutcomeSystem>(out var resolvedOutcome))
            {
                gameOutcomeSystem = resolvedOutcome;
                if (!isOutcomeSubscribed)
                {
                    gameOutcomeSystem.OnGameEnded += HandleOutcomeResolved;
                    isOutcomeSubscribed = true;
                }
            }

            return playerActionService != null && dayPlanningService != null && timeManager != null;
        }

        private void RefreshFeedback(string actionName = null)
        {
            if (feedbackText == null || timeManager == null)
            {
                return;
            }

            if (IsGameResolved())
            {
                var outcome = gameOutcomeSystem.CurrentResult;
                feedbackText.text = $"{outcome.Title}: {outcome.Message} (Day {outcome.ResolvedOnDay})";
                return;
            }

            var workText = playerActionService == null
                ? string.Empty
                : $" | Work left: {playerActionService.RemainingWorkActionsThisWeek}";

            if (string.IsNullOrEmpty(actionName))
            {
                feedbackText.text = $"Day {timeManager.Day} - {timeManager.Hour:00}:00{workText}";
                return;
            }

            feedbackText.text = $"{actionName}. Day {timeManager.Day} - {timeManager.Hour:00}:00{workText}";
        }

        private void RefreshPlanPreview()
        {
            if (planPreviewText == null || dayPlanningService == null)
            {
                return;
            }

            var plan = dayPlanningService.CurrentPlan;
            var sb = new StringBuilder();
            sb.Append("Plan ").Append(plan.BlockCount).Append('/').Append(DayPlan.MaxBlocksPerDay).Append(": ");

            if (plan.BlockCount == 0)
            {
                sb.Append('-');
            }
            else
            {
                for (var i = 0; i < plan.Blocks.Count; i++)
                {
                    sb.Append(i + 1).Append('.').Append(plan.Blocks[i]);
                    if (i < plan.Blocks.Count - 1)
                    {
                        sb.Append(" | ");
                    }
                }
            }

            var sim = dayPlanningService.SimulatePlan();
            if (sim.Notes.Count > 0)
            {
                sb.Append("  [").Append(sim.Notes[0]).Append(']');
            }

            planPreviewText.text = sb.ToString();
        }

        private void HandleOutcomeResolved(GameOutcomeResult _)
        {
            RefreshActionAvailability();
            RefreshFeedback();
        }

        private void RefreshActionAvailability()
        {
            var canAct = !IsGameResolved() && !HasActiveEvent() && !isInteractiveChallengePending;
            var canPlan = canAct && dayPlanningService != null;

            if (studyButton != null)
            {
                studyButton.interactable = canAct;
            }

            if (sleepButton != null)
            {
                sleepButton.interactable = canAct;
            }

            if (workButton != null)
            {
                workButton.interactable = canAct && (playerActionService == null || playerActionService.CanWorkThisWeek);
            }

            if (adminButton != null)
            {
                adminButton.interactable = canAct;
            }

            if (socializeButton != null)
            {
                socializeButton.interactable = canAct;
            }

            if (waitButton != null)
            {
                waitButton.interactable = canAct;
            }

            if (endDayButton != null)
            {
                endDayButton.interactable = !IsGameResolved() && !HasActiveEvent();
            }

            if (planStudyButton != null)
            {
                planStudyButton.interactable = canPlan;
            }

            if (planWorkButton != null)
            {
                planWorkButton.interactable = canPlan && (playerActionService == null || playerActionService.CanWorkThisWeek);
            }

            if (planSleepButton != null)
            {
                planSleepButton.interactable = canPlan;
            }

            if (planSocializeButton != null)
            {
                planSocializeButton.interactable = canPlan;
            }

            if (planWaitButton != null)
            {
                planWaitButton.interactable = canPlan;
            }

            if (planAdminButton != null)
            {
                planAdminButton.interactable = canPlan;
            }

            if (runPlanButton != null)
            {
                runPlanButton.interactable = canPlan && dayPlanningService != null && dayPlanningService.CurrentPlan.BlockCount > 0;
            }

            if (clearPlanButton != null)
            {
                clearPlanButton.interactable = canPlan && dayPlanningService != null && dayPlanningService.CurrentPlan.BlockCount > 0;
            }
        }

        private bool HasActiveEvent()
        {
            return eventManager != null && (eventManager.CurrentEvent != null || eventManager.HasPendingEvents);
        }

        private bool IsGameResolved()
        {
            return gameOutcomeSystem != null && gameOutcomeSystem.IsResolved;
        }

        private MicroChallengeOutcomeBand ResolveOutcomeBandForAction(PlannedActionType actionType)
        {
            if (microChallengeService == null || !microChallengeService.ShouldTrigger(actionType))
            {
                return MicroChallengeOutcomeBand.Good;
            }

            return microChallengeService.Resolve(actionType).OutcomeBand;
        }

        private bool TryRunInteractiveChallenge(PlannedActionType actionType, int hours)
        {
            if (microChallengeService == null || microChallengeRunner == null || microChallengeRunner.IsRunning)
            {
                return false;
            }

            if (!microChallengeService.TryBegin(actionType, out var session))
            {
                return false;
            }

            if (!microChallengeRunner.TryRun(session, (input, wasTimedOut) =>
            {
                StartCoroutine(CompleteInteractiveChallenge(actionType, hours, session, input, wasTimedOut));
            }))
            {
                microChallengeService.Abort(session, MicroChallengeAbortReason.PlayerCancel);
                return false;
            }

            isInteractiveChallengePending = true;
            RefreshActionAvailability();
            return true;
        }

        private IEnumerator CompleteInteractiveChallenge(PlannedActionType actionType, int hours, MicroChallengeSession session, MicroChallengeInput input, bool wasTimedOut)
        {
            yield return null;
            if (!TryBindServices() || playerActionService == null || microChallengeService == null)
            {
                isInteractiveChallengePending = false;
                RefreshActionAvailability();
                yield break;
            }

            var result = wasTimedOut
                ? microChallengeService.Abort(session, MicroChallengeAbortReason.Timeout)
                : microChallengeService.Complete(session, input);

            if (actionType == PlannedActionType.Admin)
            {
                playerActionService.ApplyAdmin(hours, result.OutcomeBand);
            }
            else if (actionType == PlannedActionType.Work)
            {
                playerActionService.ApplyWork(hours, result.OutcomeBand);
            }
            else
            {
                playerActionService.ApplyStudy(hours, result.OutcomeBand);
            }

            isInteractiveChallengePending = false;
            RefreshActionAvailability();
            var shortNote = string.IsNullOrWhiteSpace(result.Note) ? string.Empty : $" | {result.Note}";
            RefreshFeedback($"{actionType} {result.OutcomeBand}{shortNote}");
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
