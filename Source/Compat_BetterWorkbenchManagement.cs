using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Verse;

namespace UseBestMaterials {
    public static class BetterWorkbenchManagement {
        public static readonly bool Active =
            ModLister.AllInstalledMods.Any(m => m.PackageIdNonUnique == Strings.BWM_ID && m.Active);

        public static readonly Type Storage =
            Active ? AccessTools.TypeByName("ImprovedWorkbenches.ExtendedBillDataStorage") : null;
    }

    [HarmonyPatch]
    public static class BetterWorkbenchManagement_ExtendedBillDataStorage_MirrorBills_Patch {
        public static bool Prepare() 
            => BetterWorkbenchManagement.Active;

        public static MethodBase TargetMethod() 
            => AccessTools.Method(BetterWorkbenchManagement.Storage, "MirrorBills");

        public static void Postfix(Bill_Production sourceBill, Bill_Production destinationBill) {
            State.For(destinationBill, true).CopyFrom(sourceBill);
        }
    }
}
