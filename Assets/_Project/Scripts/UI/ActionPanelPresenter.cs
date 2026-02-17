using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed partial class ActionPanelPresenter : MonoBehaviour
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

    }
}
