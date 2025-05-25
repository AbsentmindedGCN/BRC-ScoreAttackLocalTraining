using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Reptile;

namespace ScoreAttack
{
    public class GrindTimerUI : MonoBehaviour
    {
        private TextMeshProUGUI _debtLabel;
        private TextMeshProUGUI _debtValueLabel;
        private Image _debtBar;
        private Image _debtBarBackground;

        private const float MaxTrickTimer = 1.4f;

        public void Initialize(Image chargeBarBackground, Image chargeBar, TextMeshProUGUI labelReference)
        {
            // Instantiate the background for the Grind Timer bar
            GameObject speedBarBgGO = GameObject.Instantiate(chargeBarBackground.gameObject, chargeBarBackground.transform.parent);
            speedBarBgGO.name = "GrindTimerBarBackground";
            _debtBarBackground = speedBarBgGO.GetComponent<Image>();
            _debtBarBackground.sprite = chargeBarBackground.sprite;
            _debtBarBackground.type = Image.Type.Sliced;
            _debtBarBackground.fillCenter = false;
            _debtBarBackground.color = chargeBarBackground.color;

            if (_debtBarBackground.transform.childCount > 0)
                GameObject.DestroyImmediate(_debtBarBackground.transform.GetChild(0).gameObject);

            // Instantiate the bar itself inside the background
            GameObject barClone = GameObject.Instantiate(chargeBar.gameObject, _debtBarBackground.transform);
            barClone.name = "GrindTimerBar";
            _debtBar = barClone.GetComponent<Image>();
            _debtBar.sprite = chargeBar.sprite;
            _debtBar.type = Image.Type.Filled;
            _debtBar.fillMethod = Image.FillMethod.Horizontal;
            _debtBar.fillOrigin = (int)Image.OriginHorizontal.Left;
            _debtBar.fillAmount = 0f;

            // Configure the background's RectTransform:
            RectTransform bgRT = _debtBarBackground.rectTransform;
            bgRT.sizeDelta = Vector2.one * 9f;
            bgRT.anchoredPosition = new Vector2(bgRT.anchoredPosition.x, -18.0f);

            RectTransform barRT = _debtBar.rectTransform;
            barRT.sizeDelta = Vector2.one * 9f;
            barRT.anchoredPosition = Vector2.zero;

            // Create the labels for Debt text and value
            _debtLabel = new GameObject("GrindTimerText").AddComponent<TextMeshProUGUI>();
            _debtLabel.font = labelReference.font;
            _debtLabel.fontMaterial = labelReference.fontMaterial;
            _debtLabel.fontSize = 24;
            _debtLabel.alignment = TextAlignmentOptions.Left;
            _debtLabel.enableWordWrapping = false;
            _debtLabel.color = Color.white;
            _debtLabel.enableAutoSizing = false;
            _debtLabel.outlineWidth = 0f;
            _debtLabel.characterSpacing = 3.5f;

            RectTransform debtLabelRT = _debtLabel.rectTransform;
            debtLabelRT.SetParent(labelReference.transform.parent, false);
            debtLabelRT.anchorMin = labelReference.rectTransform.anchorMin;
            debtLabelRT.anchorMax = labelReference.rectTransform.anchorMax;
            debtLabelRT.pivot = labelReference.rectTransform.pivot;
            debtLabelRT.localPosition = labelReference.transform.localPosition + new Vector3(-54f, 6f, 0);
            debtLabelRT.sizeDelta = new Vector2(45f, debtLabelRT.sizeDelta.y);

            _debtValueLabel = new GameObject("GrindTimerValue").AddComponent<TextMeshProUGUI>();
            _debtValueLabel.font = labelReference.font;
            _debtValueLabel.fontMaterial = labelReference.fontMaterial;
            _debtValueLabel.fontSize = 24;
            _debtValueLabel.alignment = TextAlignmentOptions.Right;
            _debtValueLabel.enableWordWrapping = false;
            _debtValueLabel.color = Color.white;
            _debtValueLabel.enableAutoSizing = false;
            _debtValueLabel.outlineWidth = 0f;
            _debtValueLabel.characterSpacing = 10f;

            RectTransform valueRT = _debtValueLabel.rectTransform;
            valueRT.SetParent(labelReference.transform.parent, false);
            valueRT.anchorMin = labelReference.rectTransform.anchorMin;
            valueRT.anchorMax = labelReference.rectTransform.anchorMax;
            valueRT.pivot = labelReference.rectTransform.pivot;
            valueRT.localPosition = labelReference.transform.localPosition + new Vector3(74f, 6f, 0);
            valueRT.sizeDelta = new Vector2(50f, valueRT.sizeDelta.y);

            _debtBarBackground.transform.SetSiblingIndex(chargeBarBackground.transform.GetSiblingIndex());
            _debtBar.transform.SetSiblingIndex(_debtBarBackground.transform.GetSiblingIndex() + 1);

            _debtLabel.gameObject.SetActive(false);
            _debtValueLabel.gameObject.SetActive(false);
            _debtBar.gameObject.SetActive(false);
            _debtBarBackground.gameObject.SetActive(false);
        }

        private void Start()
        {
            //InvokeRepeating(nameof(CheckSoftGoatMode), 0f, 0.5f);
        }

        private void CheckSoftGoatMode()
        {
            RectTransform bgRT = _debtBarBackground.rectTransform;
            if (AppGrindDebt.ViewerMode == GrindDebtDisplayMode.SoftGoat)
            {
                bgRT.anchoredPosition = new Vector2(bgRT.anchoredPosition.x, -16.0f);
            }
            else
            {
                bgRT.anchoredPosition = new Vector2(bgRT.anchoredPosition.x, -9.0f);
            }
        }

        void Update()
        {
            var mode = AppGrindDebt.ViewerMode;

            bool showBar = mode == GrindDebtDisplayMode.Bar || mode == GrindDebtDisplayMode.Both || mode == GrindDebtDisplayMode.SoftGoat;
            bool showValue = mode == GrindDebtDisplayMode.Value || mode == GrindDebtDisplayMode.Both || mode == GrindDebtDisplayMode.SoftGoat;

            _debtLabel?.gameObject.SetActive(showValue);
            _debtValueLabel?.gameObject.SetActive(showValue);
            _debtBar?.gameObject.SetActive(showBar);
            _debtBarBackground?.gameObject.SetActive(showBar);

            if (!showBar && !showValue) return;

            if (WorldHandler.instance == null) return;

            var ply = WorldHandler.instance.GetCurrentPlayer();
            if (ply == null || ply.grindAbility == null) return;

            float t = ply.grindAbility.trickTimer;

            //var ply = WorldHandler.instance.GetCurrentPlayer();
            //float t = ply?.grindAbility?.trickTimer ?? 0f;

            UpdateVisuals(t);
        }

        private void UpdateVisuals(float t)
        {
            UpdateLabel(t);
            UpdateBar(t);
        }

        private void UpdateLabel(float t)
        {
            if (_debtLabel == null || _debtValueLabel == null) return;

            _debtLabel.text = "Debt:";

            // Ensure that t is clamped to avoid negative values
            float clampedT = Mathf.Max(t, 0f); // Ensure no negative debt value

            // Truncate the value to 1 decimal place
            float truncatedT = Mathf.Floor(clampedT * 10) / 10f;  // Truncate to 1 decimal place
            string formattedValue = $"<mspace=0.55em>{truncatedT:0.0}</mspace>";

            if (truncatedT >= 0.2f)
            {
                if (AppGrindDebt.ColorMode == GrindDebtColorMode.Flashing)
                {
                    bool flashRed = Mathf.FloorToInt(Time.time * 4) % 2 == 0;
                    string color = flashRed ? "red" : "white";
                    _debtValueLabel.text = $"<color={color}>{formattedValue}</color>";
                }
                else if (AppGrindDebt.ColorMode == GrindDebtColorMode.Red)
                {
                    _debtValueLabel.text = $"<color=red>{formattedValue}</color>";
                }
                else
                {
                    _debtValueLabel.text = $"<color=white>{formattedValue}</color>";
                }
            }
            else
            {
                //_debtValueLabel.text = $"<color=green>{formattedValue}</color>";

                if (AppGrindDebt.ColorMode != GrindDebtColorMode.White)
                {
                    _debtValueLabel.text = $"<color=green>{formattedValue}</color>";
                }
                else
                {
                    _debtValueLabel.text = $"<color=white>{formattedValue}</color>";
                }

            }

        }

        private void UpdateBar(float t)
        {
            if (_debtBar == null || _debtBarBackground == null) return;

            // Clamp t to ensure no negative values are passed
            float clampedT = Mathf.Max(t, 0f); // Prevent negative values
            _debtBar.fillAmount = clampedT / MaxTrickTimer;

            // Truncate the value to 1 decimal place
            float truncatedT = Mathf.Floor(clampedT * 10) / 10f;

            if (truncatedT >= 0.2f)
            {
                if (AppGrindDebt.ColorMode == GrindDebtColorMode.Flashing)
                {
                    bool flashRed = Mathf.FloorToInt(Time.time * 4) % 2 == 0;
                    _debtBar.color = flashRed ? Color.red : Color.white;
                }
                else if (AppGrindDebt.ColorMode == GrindDebtColorMode.Red)
                {
                    _debtBar.color = Color.red;
                }
                else
                {
                    _debtBar.color = Color.white;
                }
            }
            else
            {
                if (AppGrindDebt.ColorMode != GrindDebtColorMode.White)
                {
                    _debtBar.color = Color.green;
                }
                else
                {
                    _debtBar.color = Color.white;
                }
            }
        }
    }
}
