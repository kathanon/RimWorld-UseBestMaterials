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
            var before = availableThings.Select(t => t.Label).Join();
            if (state?.Active ?? false) {
                availableThings.Sort(state.ComparerFor(rootCell));
                alreadySorted = true;
            }
            var after = availableThings.Select(t => t.Label).Join();
            Log.Message($"FindBestIngredients: state.Active = {state?.Active.ToString() ?? "null"}\n{before}\n{after}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Bill), nameof(Bill.ExposeData))]
        public static void Bill_ExposeData(Bill __instance) {
            State.For(__instance, Scribe.mode == LoadSaveMode.LoadingVars)?.ExposeData();
        }


        // Bill dialog addition
        private static State curGUI = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
        public static void DoWindowContents_pre(Bill_Production ___bill) 
            => curGUI = State.For(___bill, true);

        [HarmonyPostfix]
        [HarmonyPatch(typeof(Dialog_BillConfig), nameof(Dialog_BillConfig.DoWindowContents))]
        public static void DoWindowContents_post(Bill_Production ___bill) 
            => curGUI = null;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ThingFilterUI), nameof(ThingFilterUI.DoThingFilterConfigWindow))]
        public static void DoThingFilterConfigWindow(ref Rect rect) {
            if (curGUI != null && curGUI.Valid) {
                var r = rect.BottomPartPixels(State.GUIHeight);
                r.y -= State.GUIGap;
                curGUI.DoGUI(r);
                rect.height -= State.GUISpace;
            }
        }
    }
}
