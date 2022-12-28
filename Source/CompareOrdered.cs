using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace UseBestMaterials {
    public class CompareOrdered : ICompareFor, IExposable {
        private List<ICompareFor> parts = new List<ICompareFor>();

        private string tooltip;
        private bool dirty = true;

        public CompareOrdered() {}

        public CompareOrdered(params ICompareFor[] parts) {
            this.parts.AddRange(parts);
        }

        public string Label => $"{parts[0].Label} (+)";

        public string Tooltip {
            get {
                if (dirty) {
                    tooltip = parts
                        .Select(p => p.Tooltip.Replace("\n", ""))
                        .Join(delimiter: ",\nthen by");
                    dirty = false;
                }
                return tooltip;
            }
        }

        public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY) {
            foreach (var part in parts) {
                int res = part.Compare(thing, stuffX, stuffY);
                if (res != 0) return res;
            }
            return 0;
        }

        public ICompareFor Copy() 
            => new CompareOrdered { 
                parts = parts.Select(p => p.Copy()).ToList(),
            };

        public bool AllMatches(Func<ICompareFor, bool> matches) => parts.All(matches);

        public void ExposeData() {
            Scribe_Collections.Look(ref parts, "parts", LookMode.Deep);
            dirty = true;
        }
    }
}
