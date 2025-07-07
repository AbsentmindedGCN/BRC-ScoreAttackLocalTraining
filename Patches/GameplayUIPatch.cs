using HarmonyLib;
using Reptile;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ScoreAttack.Patches
{
    [HarmonyPatch(typeof(GameplayUI))]
    public class GameplayUIPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(nameof(GameplayUI.Init))]
        public static void Init_Postfix(GameplayUI __instance)
        {
            Image chargeBar = Traverse.Create(__instance).Field<Image>("chargeBar").Value;
            TextMeshProUGUI tricksInComboLabel = Traverse.Create(__instance).Field<TextMeshProUGUI>("tricksInComboLabel").Value;

            GameObject chargeBarBackground = chargeBar.transform.parent.gameObject;

            // Clone the background bar (like Speedometer)
            GameObject debtBarGO = Object.Instantiate(chargeBarBackground, chargeBarBackground.transform.parent);
            Image debtBarBackground = debtBarGO.GetComponent<Image>();

            // Position it below the original charge bar
            debtBarBackground.transform.SetSiblingIndex(chargeBarBackground.transform.GetSiblingIndex());

            // Destroy the chunk (the swirl fill image)
            if (debtBarGO.transform.childCount > 0)
            {
                Object.DestroyImmediate(debtBarGO.transform.GetChild(0).gameObject);
            }

            // Get the fill image (child of background)
            Image debtBarFill = null;
            foreach (Transform t in debtBarGO.transform)
            {
                debtBarFill = t.GetComponent<Image>();
                if (debtBarFill != null) break;
            }

            // Set up the fill image
            debtBarFill.type = Image.Type.Filled;
            debtBarFill.fillMethod = Image.FillMethod.Horizontal;
            debtBarFill.fillOrigin = (int)Image.OriginHorizontal.Left;
            debtBarFill.fillAmount = 0f;
            debtBarFill.color = new Color(1f, 0f, 0f); // red fill
            debtBarFill.material = debtBarBackground.material;

            // Initialize the GrindTimer logic
            var grindUI = __instance.gameplayScreen.gameObject.AddComponent<GrindTimerUI>();
            grindUI.Initialize(debtBarBackground, debtBarFill, tricksInComboLabel);
        }
    }
}