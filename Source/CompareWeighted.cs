using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using static UnityEngine.Random;

namespace UseBestMaterials {
    public class CompareWeighted : ICompareFor, IExposable {
        private List<StatDef> stats = new List<StatDef>();
        private List<float> weights = new List<float>();
        private string label = Strings.CompareWeightedLabel;

        private string tooltip;
        private bool dirty = true;

        public string Label => label;

        public string Tooltip {
            get {
                if (dirty) {
                    tooltip = "Weighted: \n = " + stats
                        .Select((s, i) => $"{weights[i]} x {s.LabelCap}")
                        .Join(delimiter: "\n + ");
                    dirty = false;
                }
                return tooltip;
            }
        }

        public CompareWeighted() {}

        public CompareWeighted(StatDef stat) {
            stats.Add(stat);
            weights.Add(1f);
        }

        public CompareWeighted((StatDef def, float weight)[] stats, string label = null) {
            this.stats.AddRange(stats.Select(s => s.def));
            weights.AddRange(stats.Select(s => s.weight));
            if (label != null) this.label = label;
        }

        public ICompareFor Copy()
            => new CompareWeighted() {
                stats = stats.ToList(),
                weights = weights.ToList(),
                label = label,
            };

        public bool Matches((StatDef def, float weight)[] values) {
            if (values.Length != stats.Count) return false;
            var set = stats.ToHashSet();
            return values.All(v => set.Contains(v.def));
        }

        private float WeightedValue(ThingDef thing, ThingDef stuff) 
            => stats.Select((s, i) => CompareSingle.Value(thing, stuff, s) * weights[i]).Sum();

        public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY) 
            => Math.Sign(WeightedValue(thing, stuffY) - WeightedValue(thing, stuffX));

        public void ExposeData() {
            Scribe_Collections.Look(ref stats, "stats", LookMode.Def);
            Scribe_Collections.Look(ref weights, "weights", LookMode.Value);
            Scribe_Values.Look(ref label, "label");
            dirty = true;
        }
    }
}
