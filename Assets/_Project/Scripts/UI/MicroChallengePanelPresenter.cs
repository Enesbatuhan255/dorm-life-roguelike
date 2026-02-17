using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class MicroChallengePanelPresenter : MonoBehaviour, IMicroChallengeInteractiveRunner
    {
        private enum InteractiveMode
        {
            None,
            Study,
            Admin,
            Work
        }

        [SerializeField] private Text statusText;
        [SerializeField] private GameObject interactiveRoot;
        [SerializeField] private Text interactiveTitleText;
        [SerializeField] private Text interactiveTimerText;
        [SerializeField] private Text interactiveHitText;
        [SerializeField] private Text interactiveInfoText;
        [SerializeField] private Button interactiveHitButton;
        [SerializeField] private Button interactiveAdminOptionAButton;
        [SerializeField] private Button interactiveAdminOptionBButton;
        [SerializeField] private Button interactiveAdminOptionCButton;

        [Header("Study Focus Tuning")]
        [SerializeField] private int targetHitsForPerfect = 24;
        [SerializeField] private int targetHitsForGood = 14;
        [SerializeField] private float minHitIntervalSeconds = 0.11f;

        [Header("Admin Sort Tuning")]
        [SerializeField] private int targetAdminCyclesForPerfect = 5;
        [SerializeField] private int targetAdminCyclesForGood = 3;

        [Header("Work Rhythm Tuning")]
        [SerializeField] private int targetWorkHitsForPerfect = 8;
        [SerializeField] private int targetWorkHitsForGood = 5;
        [SerializeField] private float workRhythmWindowSeconds = 0.25f;
        [SerializeField] private float workRhythmCycleSeconds = 0.9f;

        private IMicroChallengeService microChallengeService;
        private bool isSubscribed;
        private bool isRunning;
        private InteractiveMode currentMode;

        private int currentHits;
        private int spamFilteredHits;
        private float lastAcceptedHitTimestamp;

        private int adminCorrectClicks;
        private int adminMistakes;
        private int adminExpectedValue;
        private int adminValueA;
        private int adminValueB;
        private int adminValueC;
        private int studyStreak;
        private int workSuccessfulHits;
        private int workMistakes;
        private int workTotalPresses;
        private float workRhythmTimer;

        private Action<MicroChallengeInput, bool> activeCompletion;

        public bool IsRunning => isRunning;

        private void Start()
        {
            EnsureStatusText();
            EnsureInteractiveUi();
            TryBindService();
        }

        private void Update()
        {
            if (microChallengeService == null)
            {
                TryBindService();
            }
        }

        public bool TryRun(MicroChallengeSession session, Action<MicroChallengeInput, bool> onCompleted)
        {
            if (isRunning || onCompleted == null)
            {
                return false;
            }

            EnsureInteractiveUi();
            if (interactiveRoot == null || interactiveHitButton == null || interactiveAdminOptionAButton == null || interactiveAdminOptionBButton == null || interactiveAdminOptionCButton == null)
            {
                return false;
            }

            activeCompletion = onCompleted;
            switch (session.ChallengeType)
            {
                case MicroChallengeType.StudyFocus:
                    StartCoroutine(RunStudyFocusChallenge(session));
                    return true;
                case MicroChallengeType.AdminSort:
                    StartCoroutine(RunAdminSortChallenge(session));
                    return true;
                case MicroChallengeType.WorkRhythm:
                    StartCoroutine(RunWorkRhythmChallenge(session));
                    return true;
                default:
                    activeCompletion = null;
                    return false;
            }
        }

        private void OnDestroy()
        {
            if (microChallengeService != null && isSubscribed)
            {
                microChallengeService.OnChallengeStarted -= HandleChallengeStarted;
                microChallengeService.OnChallengeResolved -= HandleChallengeResolved;
                isSubscribed = false;
            }

            if (interactiveHitButton != null)
            {
                interactiveHitButton.onClick.RemoveListener(HandleInteractiveHit);
            }

            if (interactiveAdminOptionAButton != null)
            {
                interactiveAdminOptionAButton.onClick.RemoveListener(HandleAdminOptionA);
            }

            if (interactiveAdminOptionBButton != null)
            {
                interactiveAdminOptionBButton.onClick.RemoveListener(HandleAdminOptionB);
            }

            if (interactiveAdminOptionCButton != null)
            {
                interactiveAdminOptionCButton.onClick.RemoveListener(HandleAdminOptionC);
            }
        }

        private void TryBindService()
        {
            if (!ServiceLocator.TryGet<IMicroChallengeService>(out var resolved))
            {
                return;
            }

            microChallengeService = resolved;
            if (!isSubscribed)
            {
                microChallengeService.OnChallengeStarted += HandleChallengeStarted;
                microChallengeService.OnChallengeResolved += HandleChallengeResolved;
                isSubscribed = true;
            }
        }

        private void HandleChallengeStarted(PlannedActionType actionType, MicroChallengeSession session)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = $"Challenge started: {actionType} ({session.ChallengeType}) | Time {session.TimeLimitSeconds:0}s";
        }

        private void HandleChallengeResolved(PlannedActionType actionType, MicroChallengeResult result)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.color = GetBandColor(result.OutcomeBand);
            statusText.text = $"Challenge: {actionType} -> {result.OutcomeBand} ({result.EffectiveScore:0.00}) | {result.Note}";
        }

        private IEnumerator RunStudyFocusChallenge(MicroChallengeSession session)
        {
            isRunning = true;
            currentMode = InteractiveMode.Study;
            currentHits = 0;
            spamFilteredHits = 0;
            lastAcceptedHitTimestamp = -999f;
            studyStreak = 0;
            SetStudyUiVisible(true);
            SetAdminUiVisible(false);
            interactiveRoot.SetActive(true);
            UpdateStudyStateText();
            SetInfoText("Ritmi koru. Spam ceza getirir.");
            SetInfoColor(new Color(0.80f, 0.90f, 1f, 1f));
            interactiveTitleText.text = "Study Focus: Hit as many times as you can";

            var duration = Mathf.Max(2f, session.TimeLimitSeconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var remaining = Mathf.Max(0f, duration - elapsed);
                interactiveTimerText.text = $"Time: {remaining:0.0}s";
                yield return null;
            }

            interactiveRoot.SetActive(false);
            isRunning = false;
            currentMode = InteractiveMode.None;

            var perfectTarget = Mathf.Max(targetHitsForPerfect, targetHitsForGood + 1);
            var goodTarget = Mathf.Max(1, targetHitsForGood);
            var normalizedHits = Mathf.Clamp01(currentHits / (float)perfectTarget);
            var speedQuality = Mathf.Clamp01((currentHits - goodTarget) / (float)(perfectTarget - goodTarget));
            var mistakes = Mathf.Max(0, perfectTarget - currentHits) + spamFilteredHits;
            var input = new MicroChallengeInput(normalizedHits, speedQuality, currentHits, mistakes);
            CompleteInteractive(input);
        }

        private IEnumerator RunAdminSortChallenge(MicroChallengeSession session)
        {
            isRunning = true;
            currentMode = InteractiveMode.Admin;
            adminCorrectClicks = 0;
            adminMistakes = 0;
            adminExpectedValue = 1;
            SetupAdminOptionValues();
            SetStudyUiVisible(false);
            SetAdminUiVisible(true);
            interactiveRoot.SetActive(true);
            UpdateAdminStateText();
            SetInfoText("Dogru siralamayi koru: 1 -> 2 -> 3");
            SetInfoColor(new Color(0.80f, 0.90f, 1f, 1f));
            interactiveTitleText.text = "Admin Sort: Click 1 -> 2 -> 3 repeatedly";

            var duration = Mathf.Max(2f, session.TimeLimitSeconds);
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var remaining = Mathf.Max(0f, duration - elapsed);
                interactiveTimerText.text = $"Time: {remaining:0.0}s";
                yield return null;
            }

            interactiveRoot.SetActive(false);
            isRunning = false;
            currentMode = InteractiveMode.None;

            var perfectCycles = Mathf.Max(targetAdminCyclesForPerfect, targetAdminCyclesForGood + 1);
            var goodCycles = Mathf.Max(1, targetAdminCyclesForGood);
            var cyclesCompleted = adminCorrectClicks / 3;
            var accuracy = Mathf.Clamp01(cyclesCompleted / (float)perfectCycles);
            var speed = Mathf.Clamp01((adminCorrectClicks - (goodCycles * 3f)) / ((perfectCycles - goodCycles) * 3f));
            var misses = Mathf.Max(0, (perfectCycles * 3) - adminCorrectClicks) + adminMistakes;
            var input = new MicroChallengeInput(accuracy, speed, adminCorrectClicks, misses);
            CompleteInteractive(input);
        }

        private IEnumerator RunWorkRhythmChallenge(MicroChallengeSession session)
        {
            isRunning = true;
            currentMode = InteractiveMode.Work;
            workSuccessfulHits = 0;
            workMistakes = 0;
            workTotalPresses = 0;
            workRhythmTimer = 0f;
            SetStudyUiVisible(true);
            SetAdminUiVisible(false);
            interactiveRoot.SetActive(true);
            UpdateWorkStateText();
            SetInfoText("Sadece GO penceresinde tikla.");
            interactiveTitleText.text = "Work Rhythm: Press Hit on GO";

            var duration = Mathf.Max(2f, session.TimeLimitSeconds);
            var elapsed = 0f;
            var cycle = Mathf.Max(0.35f, workRhythmCycleSeconds);
            var window = Mathf.Clamp(workRhythmWindowSeconds, 0.05f, cycle - 0.05f);
            while (elapsed < duration)
            {
                var delta = Time.unscaledDeltaTime;
                elapsed += delta;
                workRhythmTimer += delta;
                if (workRhythmTimer >= cycle)
                {
                    workRhythmTimer -= cycle;
                }

                var inWindow = workRhythmTimer <= window;
                var remaining = Mathf.Max(0f, duration - elapsed);
                interactiveTimerText.text = $"Time: {remaining:0.0}s";
                SetInfoText(inWindow ? "GO!" : "WAIT...");
                SetInfoColor(inWindow ? new Color(0.55f, 1f, 0.55f, 1f) : new Color(1f, 0.60f, 0.60f, 1f));
                yield return null;
            }

            interactiveRoot.SetActive(false);
            isRunning = false;
            currentMode = InteractiveMode.None;

            var perfectTarget = Mathf.Max(targetWorkHitsForPerfect, targetWorkHitsForGood + 1);
            var goodTarget = Mathf.Max(1, targetWorkHitsForGood);
            var accuracy = workTotalPresses > 0
                ? Mathf.Clamp01(workSuccessfulHits / (float)workTotalPresses)
                : 0f;
            var speed = Mathf.Clamp01((workSuccessfulHits - goodTarget) / (float)(perfectTarget - goodTarget));
            var misses = Mathf.Max(0, perfectTarget - workSuccessfulHits) + workMistakes;
            var input = new MicroChallengeInput(accuracy, speed, workSuccessfulHits, misses);
            CompleteInteractive(input);
        }

        private void CompleteInteractive(MicroChallengeInput input)
        {
            var completed = activeCompletion;
            activeCompletion = null;
            completed?.Invoke(input, false);
        }

        private void HandleInteractiveHit()
        {
            if (!isRunning)
            {
                return;
            }

            if (currentMode == InteractiveMode.Work)
            {
                HandleWorkRhythmHit();
                return;
            }

            if (currentMode != InteractiveMode.Study)
            {
                return;
            }

            var now = Time.unscaledTime;
            if (now - lastAcceptedHitTimestamp < Mathf.Max(0.02f, minHitIntervalSeconds))
            {
                spamFilteredHits++;
                studyStreak = 0;
                SetInfoText("Cok hizli. Ritmi yavaslat.");
                SetInfoColor(new Color(1f, 0.70f, 0.55f, 1f));
                return;
            }

            lastAcceptedHitTimestamp = now;
            currentHits++;
            studyStreak++;
            UpdateStudyStateText();
            if (studyStreak >= 6)
            {
                SetInfoText("Temiz seri! Devam et.");
                SetInfoColor(new Color(0.55f, 1f, 0.55f, 1f));
            }
        }

        private void HandleWorkRhythmHit()
        {
            var cycle = Mathf.Max(0.35f, workRhythmCycleSeconds);
            var window = Mathf.Clamp(workRhythmWindowSeconds, 0.05f, cycle - 0.05f);
            var inWindow = workRhythmTimer <= window;
            workTotalPresses++;
            if (inWindow)
            {
                workSuccessfulHits++;
            }
            else
            {
                workMistakes++;
            }

            UpdateWorkStateText();
        }

        private void HandleAdminOptionA()
        {
            HandleAdminOptionClicked(adminValueA);
        }

        private void HandleAdminOptionB()
        {
            HandleAdminOptionClicked(adminValueB);
        }

        private void HandleAdminOptionC()
        {
            HandleAdminOptionClicked(adminValueC);
        }

        private void HandleAdminOptionClicked(int value)
        {
            if (!isRunning || currentMode != InteractiveMode.Admin)
            {
                return;
            }

            if (value == adminExpectedValue)
            {
                adminCorrectClicks++;
                adminExpectedValue++;
                if (adminExpectedValue > 3)
                {
                    adminExpectedValue = 1;
                }

                if (adminCorrectClicks % 6 == 0)
                {
                    SetInfoText("Evrak akisi temiz gidiyor.");
                    SetInfoColor(new Color(0.55f, 1f, 0.55f, 1f));
                }
            }
            else
            {
                adminMistakes++;
                SetInfoText("Yanlis sira. Sonraki sayiya odaklan.");
                SetInfoColor(new Color(1f, 0.70f, 0.55f, 1f));
            }

            UpdateAdminStateText();
        }

        private void UpdateStudyStateText()
        {
            if (interactiveHitText != null)
            {
                interactiveHitText.text = $"Hits: {currentHits} (Perfect {targetHitsForPerfect}+)";
            }
        }

        private void UpdateAdminStateText()
        {
            if (interactiveHitText == null)
            {
                return;
            }

            var cycles = adminCorrectClicks / 3;
            interactiveHitText.text = $"Cycles: {cycles} (Perfect {targetAdminCyclesForPerfect}+) | Next: {adminExpectedValue}";
        }

        private void UpdateWorkStateText()
        {
            if (interactiveHitText == null)
            {
                return;
            }

            var accuracy = workTotalPresses > 0
                ? Mathf.RoundToInt((workSuccessfulHits / (float)workTotalPresses) * 100f)
                : 0;
            interactiveHitText.text = $"Hits: {workSuccessfulHits} (Perfect {targetWorkHitsForPerfect}+) | Acc: {accuracy}%";
        }

        private void SetupAdminOptionValues()
        {
            var values = new[] { 1, 2, 3 };
            for (var i = 0; i < values.Length; i++)
            {
                var swap = UnityEngine.Random.Range(i, values.Length);
                (values[i], values[swap]) = (values[swap], values[i]);
            }

            adminValueA = values[0];
            adminValueB = values[1];
            adminValueC = values[2];

            SetButtonLabel(interactiveAdminOptionAButton, adminValueA.ToString());
            SetButtonLabel(interactiveAdminOptionBButton, adminValueB.ToString());
            SetButtonLabel(interactiveAdminOptionCButton, adminValueC.ToString());
        }

        private void SetStudyUiVisible(bool visible)
        {
            if (interactiveHitButton != null)
            {
                interactiveHitButton.gameObject.SetActive(visible);
            }
        }

        private void SetAdminUiVisible(bool visible)
        {
            if (interactiveAdminOptionAButton != null)
            {
                interactiveAdminOptionAButton.gameObject.SetActive(visible);
            }

            if (interactiveAdminOptionBButton != null)
            {
                interactiveAdminOptionBButton.gameObject.SetActive(visible);
            }

            if (interactiveAdminOptionCButton != null)
            {
                interactiveAdminOptionCButton.gameObject.SetActive(visible);
            }
        }

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
