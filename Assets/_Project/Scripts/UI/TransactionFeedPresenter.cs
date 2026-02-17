using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DormLifeRoguelike
{
    public sealed class TransactionFeedPresenter : MonoBehaviour
    {
        [SerializeField] private Text feedText;
        [SerializeField] private int maxEntries = 5;

        private readonly Queue<string> entries = new Queue<string>();
        private IEconomySystem economySystem;
        private bool isSubscribed;

        private void Start()
        {
            EnsureFeedText();
            TryBindEconomy();
            RefreshText();
        }

        private void Update()
        {
            if (economySystem == null)
            {
                TryBindEconomy();
            }
        }

        private void OnDestroy()
        {
            if (economySystem != null && isSubscribed)
            {
                economySystem.OnTransactionApplied -= HandleTransactionApplied;
                isSubscribed = false;
            }
        }

        private void TryBindEconomy()
        {
            if (!ServiceLocator.TryGet<IEconomySystem>(out var resolved))
            {
                return;
            }

            economySystem = resolved;
            if (!isSubscribed)
            {
                economySystem.OnTransactionApplied += HandleTransactionApplied;
                isSubscribed = true;
            }
        }

        private void HandleTransactionApplied(float amount, string reason)
        {
            var sign = amount >= 0f ? "+" : string.Empty;
            var normalizedReason = string.IsNullOrWhiteSpace(reason) ? "Transaction" : reason;
            var line = $"{sign}{amount:0.#} {normalizedReason}";

            entries.Enqueue(line);
            var cap = Mathf.Max(1, maxEntries);
            while (entries.Count > cap)
            {
                entries.Dequeue();
            }

            RefreshText();
        }

        private void RefreshText()
        {
            if (feedText == null)
            {
                return;
            }

            if (entries.Count == 0)
            {
                feedText.text = "Transactions:\n-";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Transactions:");
            foreach (var line in entries)
            {
                sb.AppendLine(line);
            }

            feedText.text = sb.ToString().TrimEnd();
        }

        private void EnsureFeedText()
        {
            if (feedText != null)
            {
                return;
            }

            var existing = transform.Find("TransactionFeedText");
            if (existing != null && existing.TryGetComponent<Text>(out var existingText))
            {
                feedText = existingText;
                return;
            }

            var go = new GameObject("TransactionFeedText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(transform, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(360f, -12f);
            rect.sizeDelta = new Vector2(360f, 140f);

            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 16;
            text.alignment = TextAnchor.UpperLeft;
            text.color = Color.white;
            text.text = "Transactions:\n-";

            feedText = text;
        }
    }
}
