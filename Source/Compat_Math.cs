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
    public static class Compat_Math {

        public static readonly bool Active =
            ModLister.AllInstalledMods.Any(m => m.PackageIdNonUnique == Strings.MATH_ID && m.Active);

        public static readonly Type Dialog =
            Active ? Find("CrunchyDuck.Math.", "Dialog_MathBillConfig", "", "Dialogs.") : null;

        private static Type Find(string prefix, string name, params string[] paths) {
            foreach (var path in paths) {
                var type = AccessTools.TypeByName(prefix + path + name);
                if (type != null) return type;
            }
            return null;
        }
    }

    [HarmonyPatch]
    public static class MathCompat_DoWindowContents_Patch {

        [HarmonyPrepare]
        public static bool ShouldPatch() =>
            Compat_Math.Active;

        [HarmonyTargetMethod]
        public static MethodBase Method() =>
            AccessTools.Method(Compat_Math.Dialog, "DoWindowContents");

        [HarmonyPrefix]
        public static void DoWindowContents_pre(Bill_Production ___bill) =>
            Patches_Bill.DoWindowContents_pre(___bill);

        [HarmonyPostfix]
        public static void DoWindowContents_post() =>
            Patches_Bill.DoWindowContents_post();
    }
}
