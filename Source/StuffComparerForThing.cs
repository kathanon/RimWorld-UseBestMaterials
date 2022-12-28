using RimWorld;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    public class StuffComparerForThing : StuffComparer, IComparer<Thing>, IExposable {
        private IntVec3 origin;
        private bool hasOrigin = false;

        public StuffComparerForThing(ThingDef thing, List<StatDef> stats) 
            : base(thing, stats) {}

        public StuffComparerForThing ForCell(IntVec3 cell) {
            origin = cell;
            hasOrigin = true;
            return this;
        }

        public int Compare(Thing x, Thing y) {
            int res = Compare(x.def, y.def);
            if (res == 0 && hasOrigin) {
                res = Dist(x) - Dist(y);
            }
            return res;

            int Dist(Thing x) => (x.PositionHeld - origin).LengthHorizontalSquared;
        }

        public void ExposeData() => Scribe_Deep.Look(ref cmp, "compare");

        public void CopyFrom(StuffComparerForThing fromSC) {
            var from = fromSC.cmp;
            if ((from is CompareOrdered co) ? co.AllMatches(Matches) : Matches(from)) {
                cmp = from.Copy();
            }
        }

        private bool Matches(ICompareFor other) {
            foreach (var gen in generators) {
                if (gen.Matches(other)) {
                    return true;
                }
            }
            return false;
        }
    }
}
