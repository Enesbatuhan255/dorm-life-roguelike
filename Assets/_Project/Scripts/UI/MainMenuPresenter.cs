using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class MainMenuPresenter : MonoBehaviour
    {
        private const string GameSceneName = "SampleScene";
        private const string QuickSaveFile = "quick.json";
        private const string SaveFolderName = "DormLifeRoguelike";
        private const string SaveSubFolderName = "saves";

        // Reference image resolution used for hotspot mapping.
        private const float RefWidth = 1365f;
        private const float RefHeight = 768f;

        [Header("Visual")]
        [SerializeField] private Sprite menuBackgroundSprite;
        [SerializeField] private Texture2D menuBackgroundTexture;
        [SerializeField] private Color menuBackgroundTint = Color.white;

        private Button continueButton;
        private Image continueLockOverlay;

        private void Awake()
        {
            EnsureEventSystem();
            var canvas = EnsureCanvas();
            BuildReferenceLayout(canvas);
            RefreshContinueState();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                RefreshContinueState();
            }
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetParent(transform, false);
        }

        private Canvas EnsureCanvas()
        {
            var canvas = GetComponentInChildren<Canvas>();
            if (canvas != null)
            {
                return canvas;
            }

            var canvasObj = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            return canvas;
        }

        private void BuildReferenceLayout(Canvas canvas)
        {
            if (canvas == null)
            {
                return;
            }

            ClearChildren(canvas.transform);

            // Fullscreen background matching the provided concept art.
            var bg = new GameObject("ReferenceBackground", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            bg.transform.SetParent(canvas.transform, false);
            var bgRect = bg.GetComponent<RectTransform>();
            Stretch(bgRect);
            var raw = bg.GetComponent<RawImage>();
            raw.color = menuBackgroundTint;
            raw.texture = ResolveMenuTexture();

            // Invisible interactive regions matching the left menu list.
            CreateMenuHotspot(canvas.transform, "NewGameButton", new Rect(42f, 170f, 248f, 54f), StartNewGame, out _);
            CreateMenuHotspot(canvas.transform, "ContinueButton", new Rect(42f, 238f, 248f, 54f), ContinueGame, out continueButton);
            CreateMenuHotspot(canvas.transform, "LoadGameButton", new Rect(42f, 306f, 248f, 54f), LoadGame, out _);
            CreateMenuHotspot(canvas.transform, "SettingsButton", new Rect(42f, 374f, 248f, 54f), OpenSettings, out _);
            CreateMenuHotspot(canvas.transform, "ExitButton", new Rect(42f, 442f, 248f, 54f), ExitGame, out _);

            // Hide baked-in "AUTOSAVE DAY..." text from reference artwork.
            var autosaveMask = new GameObject("ContinueAutosaveTextMask", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            autosaveMask.transform.SetParent(canvas.transform, false);
            var autosaveMaskRect = autosaveMask.GetComponent<RectTransform>();
            ApplyReferenceRect(autosaveMaskRect, new Rect(58f, 288f, 210f, 16f));
            var autosaveMaskImage = autosaveMask.GetComponent<Image>();
            autosaveMaskImage.color = new Color(0.24f, 0.23f, 0.21f, 1f);
            autosaveMaskImage.raycastTarget = false;

            // Darken continue row if no quick save exists.
            var lockObj = new GameObject("ContinueLockOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockObj.transform.SetParent(canvas.transform, false);
            var lockRect = lockObj.GetComponent<RectTransform>();
            ApplyReferenceRect(lockRect, new Rect(42f, 238f, 248f, 54f));
            continueLockOverlay = lockObj.GetComponent<Image>();
            continueLockOverlay.color = new Color(0f, 0f, 0f, 0.35f);
        }

        private Texture ResolveMenuTexture()
        {
            if (menuBackgroundTexture != null)
            {
                return menuBackgroundTexture;
            }

            return menuBackgroundSprite != null ? menuBackgroundSprite.texture : Texture2D.blackTexture;
        }

        private void CreateMenuHotspot(
            Transform parent,
            string name,
            Rect referenceRect,
            UnityAction onClick,
            out Button button)
        {
            var buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);
            var rect = buttonObj.GetComponent<RectTransform>();
            ApplyReferenceRect(rect, referenceRect);

            var image = buttonObj.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.001f); // invisible, still raycast target

            button = buttonObj.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var overlayObj = new GameObject("FxOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObj.transform.SetParent(buttonObj.transform, false);
            var overlayRect = overlayObj.GetComponent<RectTransform>();
            Stretch(overlayRect);

            var overlayImage = overlayObj.GetComponent<Image>();
            overlayImage.raycastTarget = false;
            overlayImage.color = new Color(1f, 0.9f, 0.55f, 0f);

            var fx = buttonObj.AddComponent<MenuHotspotFx>();
            fx.Configure(button, overlayImage);
        }

        private void RefreshContinueState()
        {
            var hasQuick = HasQuickSave();
            if (continueButton != null)
            {
                continueButton.interactable = hasQuick;
            }

            if (continueLockOverlay != null)
            {
                continueLockOverlay.enabled = !hasQuick;
            }
        }

        private void StartNewGame()
        {
            GameStartRequest.Reset();
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }

        private void ContinueGame()
        {
            if (!HasQuickSave())
            {
                RefreshContinueState();
                return;
            }

            GameStartRequest.RequestQuickLoad();
            SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }

        private void LoadGame()
        {
            ContinueGame();
        }

        private static void OpenSettings()
        {
            Debug.Log("[MainMenuPresenter] Settings panel will be wired in next UI pass.");
        }

        private static void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static bool HasQuickSave()
        {
            var root = Path.Combine(Application.persistentDataPath, SaveFolderName, SaveSubFolderName);
            return File.Exists(Path.Combine(root, QuickSaveFile));
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void ApplyReferenceRect(RectTransform rect, Rect refRect)
        {
            // Convert top-left based reference pixels into normalized anchors.
            var xMin = refRect.x / RefWidth;
            var xMax = (refRect.x + refRect.width) / RefWidth;
            var yMax = 1f - (refRect.y / RefHeight);
            var yMin = 1f - ((refRect.y + refRect.height) / RefHeight);

            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private sealed class MenuHotspotFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
        {
            private Button ownerButton;
            private Image overlayImage;
            private RectTransform rectTransform;
            private float targetAlpha;
            private float currentAlpha;
            private float targetScale = 1f;
            private float currentScale = 1f;
            private bool isHovering;

            public void Configure(Button owner, Image overlay)
            {
                ownerButton = owner;
                overlayImage = overlay;
                rectTransform = transform as RectTransform;
                targetAlpha = 0f;
                currentAlpha = 0f;
            }

            public void OnPointerEnter(PointerEventData eventData)
            {
                if (!CanAnimate())
                {
                    return;
                }

                isHovering = true;
                targetAlpha = 0.17f;
            }

            public void OnPointerExit(PointerEventData eventData)
            {
                isHovering = false;
                targetAlpha = 0f;
                targetScale = 1f;
            }

            public void OnPointerDown(PointerEventData eventData)
            {
                if (!CanAnimate())
                {
                    return;
                }

                targetAlpha = 0.30f;
                targetScale = 0.985f;
            }

            public void OnPointerUp(PointerEventData eventData)
            {
                if (!CanAnimate())
                {
                    return;
                }

                targetAlpha = isHovering ? 0.17f : 0f;
                targetScale = 1f;
            }

            private void Update()
            {
                if (overlayImage == null || rectTransform == null)
                {
                    return;
                }

                if (!CanAnimate())
                {
                    targetAlpha = 0f;
                    targetScale = 1f;
                }

                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.unscaledDeltaTime * 18f);
                currentScale = Mathf.Lerp(currentScale, targetScale, Time.unscaledDeltaTime * 20f);

                var color = overlayImage.color;
                color.a = currentAlpha;
                overlayImage.color = color;
                rectTransform.localScale = Vector3.one * currentScale;
            }

            private bool CanAnimate()
            {
                return ownerButton != null && ownerButton.interactable;
            }
        }
    }
}
