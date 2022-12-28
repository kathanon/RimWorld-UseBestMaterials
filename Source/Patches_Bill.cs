using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    [HarmonyPatch]
    public static class Patches_Bill {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(WorkGiver_DoBill), "TryFindBestIngredientsInSet_NoMixHelper")]
        public static void FindBestIngredients(
                List<Thing> availableThings, ref bool alreadySorted, Bill bill, IntVec3 rootCell) {
            var state = State.For(bill);
            if (state?.Active ?? false) {
                availableThings.Sort(state.ForThingFrom(rootCell));
                alreadySorted = true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Bill), nameof(Bill.ExposeData))]
        public static void Bill_ExposeData(Bill __instance) {
            State.For(__instance, Scribe.mode == LoadSaveMode.LoadingVars)?.ExposeData();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Bill_Production), nameof(Bill_Production.Clone))]
        public static void Clone(Bill_Production __instance, Bill __result) {
            State.For(__result, true).CopyFrom(__instance);
        }


        // Bill dialog addition
        private static State curGUI = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
        public static void DoWindowContents_pre(Bill_Production ___bill) 
            => curGUI = State.For(___bill, true);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
        public static void DoWindowContents_post() 
            => curGUI = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
        public static void DoThingFilterConfigWindow(ref Rect rect) {
            if (curGUI != null && curGUI.Valid) {
                var r = rect.BottomPartPixels(State.GUIHeight);
                r.y -= State.GUIGap;
                curGUI.DoGUI(r);
                rect.height -= State.GUISpace;
                sortMoveButtons = 2;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThingDef), nameof(ThingDef.DescriptionDetailed), MethodType.Getter)]
        public static void DescriptionDetailed(ThingDef __instance, ref string __result) {
            curGUI?.AugmentIngredientDescription(__instance, ref __result);
        }


        // Sort button

        private static int sortMoveButtons = 0;
        private static Rect sortButtonRect;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Widgets), nameof(Widgets.ButtonText),
            typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(bool), typeof(TextAnchor?))]
        public static void ButtonText(ref Rect rect) {
            if (sortMoveButtons > 0) {
                sortMoveButtons--;
                bool last = sortMoveButtons == 0;

                if (last) {
                    rect = sortButtonRect;
                } else {
                    rect.width = ((int) rect.width) * 2 / 3;
                    sortButtonRect = rect;
                }
                sortButtonRect.x += sortButtonRect.width + 1f;
                
                if (last) {
                    curGUI.DoSortButton(sortButtonRect);
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ThingCategoryDef), nameof(ThingCategoryDef.SortedChildThingDefs), MethodType.Getter)]
        public static void SortedChildThingDefs(ref List<ThingDef> __result) {
            if (curGUI != null && curGUI.Valid) {
                var compare = curGUI.ForStuff;
                if (!compare.IsDefault) {
                    var list = new List<ThingDef>(__result);
                    list.Sort(compare);
                    __result = list;
                }
            }
        }


        // Info card button
        /*
        private static ThingDef thing = null;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Listing_TreeThingFilter), "DoThingDef")]
        public static void DoThingDef_pre(ThingDef tDef) {
            if (curGUI != null) thing = tDef;
        }
        // */
    }
}
