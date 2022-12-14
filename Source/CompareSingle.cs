using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace UseBestMaterials {
    public class CompareSingle : ICompareFor, IExposable {
        private StatDef stat;
        private string label;

        public CompareSingle() {}

        public CompareSingle(StatDef stat) {
            this.stat = stat;
            label = stat.LabelCap;
            int pos = label.IndexOf(" - ");
            if (pos > 0) {
                label = label.Substring(pos + 3) + " " + label.Substring(0, pos);
            }
        }

        public string Label => label;

        public string Tooltip => stat.LabelForFullStatListCap;

        public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY) 
            => Math.Sign(thing.GetStatValueAbstract(stat, stuffY) - thing.GetStatValueAbstract(stat, stuffX));

        public void ExposeData() {
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref label, "label");
        }
    }
}
