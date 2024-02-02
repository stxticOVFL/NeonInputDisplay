using HarmonyLib;
using InputDisplay.Objects;
using UnityEngine;

namespace InputDisplay
{
    [HarmonyPatch]
    internal class Hooks
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerUICardHUD), "UpdateHUD")]
        private static void PreUpdateHUD(PlayerUICardHUD __instance, ref PlayerCard card)
        {

            if (InputDisplay.Settings.SelectedColor.Value.a != 0)
                DisplayObject.currentColor = InputDisplay.Settings.SelectedColor.Value.Alpha(1f);
            else
            {
                if (card.data.discardAbility == PlayerCardData.DiscardAbility.None) // katana/fist
                    DisplayObject.currentColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                else
                    DisplayObject.currentColor = card.data.cardColor.Alpha(1f);
            }

            if (InputDisplay.Display)
                InputDisplay.Display.RefreshColor();
        }
    }
}
