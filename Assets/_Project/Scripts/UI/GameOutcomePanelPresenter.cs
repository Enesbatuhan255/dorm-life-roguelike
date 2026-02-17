using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class GameOutcomePanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private Text titleText;
        [SerializeField] private Text detailText;
        [SerializeField] private Button retryButton;

        private IGameOutcomeSystem gameOutcomeSystem;
        private bool isOutcomeSubscribed;

        private void Start()
        {
            EnsureUi();
            SetVisible(false);
            TryBindServices();
        }

        private void Update()
        {
            if (gameOutcomeSystem == null)
            {
                TryBindServices();
            }
        }

        private void OnDestroy()
        {
            if (gameOutcomeSystem != null && isOutcomeSubscribed)
            {
                gameOutcomeSystem.OnGameEnded -= HandleGameEnded;
                isOutcomeSubscribed = false;
            }

            if (retryButton != null)
            {
                retryButton.onClick.RemoveListener(HandleRetryClicked);
            }
        }

        private void TryBindServices()
        {
            if (gameOutcomeSystem == null && ServiceLocator.TryGet<IGameOutcomeSystem>(out var resolvedOutcome))
            {
                gameOutcomeSystem = resolvedOutcome;
                if (!isOutcomeSubscribed)
                {
                    gameOutcomeSystem.OnGameEnded += HandleGameEnded;
                    isOutcomeSubscribed = true;
                }
            }

            if (gameOutcomeSystem != null && gameOutcomeSystem.IsResolved)
            {
                HandleGameEnded(gameOutcomeSystem.CurrentResult);
            }
        }

        private void HandleGameEnded(GameOutcomeResult result)
        {
            EnsureUi();
            if (titleText != null)
            {
                titleText.text = string.IsNullOrWhiteSpace(result.EpilogTitle)
                    ? result.Title
                    : $"{result.Title} - {result.EpilogTitle}";
            }

            if (detailText != null)
            {
                detailText.text =
                    $"{result.Message}\nEnding: {result.EndingId}\nDebt: {result.DebtBand} | Work: {result.EmploymentState}\nDay {result.ResolvedOnDay}";
            }

            SetVisible(true);
        }

        private void HandleRetryClicked()
        {
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
        }

        private void EnsureUi()
        {
            if (panelRoot != null && titleText != null && detailText != null && retryButton != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            var parent = canvas != null ? canvas.transform : transform;
            if (panelRoot == null)
            {
                var existing = parent.Find("GameOutcomePanel");
                if (existing != null)
                {
                    panelRoot = existing.gameObject;
                    titleText = existing.Find("TitleText")?.GetComponent<Text>();
                    detailText = existing.Find("DetailText")?.GetComponent<Text>();
                    retryButton = existing.Find("RetryButton")?.GetComponent<Button>();
                    BindRetryButton();
                    return;
                }

                panelRoot = new GameObject(
                    "GameOutcomePanel",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));
                panelRoot.transform.SetParent(parent, false);
                panelRoot.transform.SetAsLastSibling();

                var panelRect = panelRoot.GetComponent<RectTransform>();
                panelRect.anchorMin = Vector2.zero;
                panelRect.anchorMax = Vector2.one;
                panelRect.offsetMin = Vector2.zero;
                panelRect.offsetMax = Vector2.zero;

                var panelImage = panelRoot.GetComponent<Image>();
                panelImage.color = new Color(0f, 0f, 0f, 0.86f);

                titleText = CreateText(panelRoot.transform, "TitleText", 48, new Vector2(0.5f, 0.62f), Color.white);
                detailText = CreateText(panelRoot.transform, "DetailText", 28, new Vector2(0.5f, 0.5f), new Color(0.92f, 0.92f, 0.92f, 1f));
                retryButton = CreateButton(panelRoot.transform, "RetryButton", "Yeni Deneme", new Vector2(0.5f, 0.35f));

                if (titleText != null)
                {
                    titleText.alignment = TextAnchor.MiddleCenter;
                }

                if (detailText != null)
                {
                    detailText.alignment = TextAnchor.MiddleCenter;
                }

                BindRetryButton();
            }
        }

        private void BindRetryButton()
        {
            if (retryButton == null)
            {
                return;
            }

            retryButton.onClick.RemoveListener(HandleRetryClicked);
            retryButton.onClick.AddListener(HandleRetryClicked);
        }

        private static Text CreateText(Transform parent, string objectName, int fontSize, Vector2 anchor, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(1400f, 180f);
            rect.anchoredPosition = Vector2.zero;

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.text = string.Empty;
            return text;
        }

        private static Button CreateButton(Transform parent, string objectName, string label, Vector2 anchor)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(280f, 64f);

            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.34f, 0.5f, 1f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            labelObject.transform.SetParent(go.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var text = labelObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 24;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.text = label;

            return button;
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(visible);
            }
        }
    }
}
