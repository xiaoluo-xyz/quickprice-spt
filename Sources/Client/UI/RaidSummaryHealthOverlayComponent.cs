using System;
using EFT.UI;
using EFT.UI.Health;
using QuickPrice.Config;
using QuickPrice.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuickPrice.UI
{
    /// <summary>
    /// 血条信息上方的战局结算信息展示
    /// </summary>
    public class RaidSummaryHealthOverlayComponent : UIElement
    {
        private const float PanelHeight = 40f;
        private const float PanelTopOffset = -37f;
        private const float FontSize = 14f;

        private TMP_Text _text;
        private long _lastBrought;
        private long _lastLoss;
        private long _lastLoot;
        private long _lastSettlement;
        private bool _lastIsInRaid;

        public static RaidSummaryHealthOverlayComponent Attach(HealthParametersPanel healthParametersPanel, TMP_Text textTemplate)
        {
            if (healthParametersPanel == null)
                return null;

            var existing = healthParametersPanel.GetComponentInChildren<RaidSummaryHealthOverlayComponent>(true);
            if (existing != null)
            {
                existing.UpdateText(true);
                return existing;
            }

            var container = new GameObject("QP_RaidSummaryHealthOverlay", typeof(RectTransform));
            container.layer = healthParametersPanel.gameObject.layer;
            container.transform.SetParent(healthParametersPanel.transform, false);

            var rect = (RectTransform)container.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(0f, PanelHeight);
            rect.anchoredPosition = new Vector2(0f, -PanelTopOffset);

            var layoutElement = container.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            var background = container.AddComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.35f);
            background.raycastTarget = false;

            var text = CreateText(textTemplate, container.transform);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(12f, 4f);
            textRect.offsetMax = new Vector2(-12f, -4f);

            var component = container.AddComponent<RaidSummaryHealthOverlayComponent>();
            component._text = text;
            component.UpdateText(true);
            return component;
        }

        public void Show()
        {
            Dispose();
            RaidSummaryMetrics.ValuesChanged += OnMetricsChanged;
            UI.AddDisposable(new Action(() => RaidSummaryMetrics.ValuesChanged -= OnMetricsChanged));
            UpdateText(true);
            ShowGameObject();
        }

        private void OnMetricsChanged()
        {
            UpdateText(false);
        }

        private void UpdateText(bool force)
        {
            if (_text == null)
                return;

            var brought = RaidSummaryMetrics.BroughtValue;
            var loss = RaidSummaryMetrics.LossValue;
            var loot = RaidSummaryMetrics.LootValue;
            var settlement = RaidSummaryMetrics.SettlementValue;
            var isInRaid = RaidSummaryMetrics.IsInRaid;

            if (!force &&
                brought == _lastBrought &&
                loss == _lastLoss &&
                loot == _lastLoot &&
                settlement == _lastSettlement &&
                isInRaid == _lastIsInRaid)
            {
                return;
            }

            _lastBrought = brought;
            _lastLoss = loss;
            _lastLoot = loot;
            _lastSettlement = settlement;
            _lastIsInRaid = isInRaid;

            _text.text = BuildDisplayText(brought, loss, loot, settlement, isInRaid);
        }

        private static string BuildDisplayText(long brought, long loss, long loot, long settlement, bool isInRaid)
        {
            string neutral = RaidSummaryMetrics.GetNeutralColor();
            if (!isInRaid)
            {
                return RaidSummaryMetrics.FormatLabelValue("带入", brought, neutral);
            }

            bool showBrought = Settings.ShowBroughtValueInRaid?.Value ?? false;
            bool showLoss = Settings.ShowLossValueInRaid?.Value ?? false;
            var parts = new System.Collections.Generic.List<string>();

            if (showBrought)
            {
                parts.Add(RaidSummaryMetrics.FormatLabelValue("带入", brought, neutral));
            }

            if (showLoss)
            {
                parts.Add(RaidSummaryMetrics.FormatLabelValue("损耗", loss, RaidSummaryMetrics.GetNegativeColor()));
            }

            parts.Add(RaidSummaryMetrics.FormatLabelValue("收获", loot, RaidSummaryMetrics.GetPositiveColor()));
            parts.Add(RaidSummaryMetrics.FormatLabelValue("结算", settlement, RaidSummaryMetrics.GetSettlementColor(settlement)));
            return string.Join("  ", parts);
        }

        private static TMP_Text CreateText(TMP_Text template, Transform parent)
        {
            TMP_Text text;
            GameObject textObject;

            if (template != null)
            {
                textObject = Instantiate(template.gameObject, parent);
                text = textObject.GetComponent<TMP_Text>();
            }
            else
            {
                textObject = new GameObject("QP_RaidSummaryText", typeof(RectTransform), typeof(TextMeshProUGUI));
                textObject.transform.SetParent(parent, false);
                text = textObject.GetComponent<TMP_Text>();
            }

            text.name = "QP_RaidSummaryText";
            text.transform.localScale = Vector3.one;
            text.raycastTarget = false;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.fontSize = Mathf.Min(text.fontSize, FontSize);
            text.richText = true;

            if (TMP_Settings.defaultFontAsset != null && text.font != TMP_Settings.defaultFontAsset)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            if (text.characterSpacing < 1f)
            {
                text.characterSpacing = 1f;
            }

            return text;
        }
    }
}
