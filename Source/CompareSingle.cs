using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace UseBestMaterials {
    public class CompareSingle : ICompareFor, IExposable {
        public static readonly HashSet<StatDef> InvertFor = new HashSet<StatDef>{
            StatDefOf.Flammability,
            StatDefOf.WorkToMake,
        };

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

        public StatDef Stat => stat;

        public string Tooltip => stat.LabelForFullStatListCap;

        public ICompareFor Copy() {
            return new CompareSingle() {
                stat = stat,
                label = label,
            };
        }

        public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY)
            => Math.Sign(Value(thing, stuffY, stat) - Value(thing, stuffX, stat));

        public static float Value(ThingDef thing, ThingDef stuff, StatDef stat) 
            => StatValue(thing, stuff, stat) * (InvertFor.Contains(stat) ? -1 : 1);

        private static float StatValue(ThingDef thing, ThingDef stuff, StatDef stat) 
            => (stuff.stuffProps != null) ? thing.GetStatValueAbstract(stat, stuff) : 0f;

        public void ExposeData() {
            Scribe_Defs.Look(ref stat, "stat");
            Scribe_Values.Look(ref label, "label");
        }
    }
}
