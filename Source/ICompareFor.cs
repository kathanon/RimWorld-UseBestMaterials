using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace UseBestMaterials {
    public interface ICompareFor {
        public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY);
        ICompareFor Copy();

        public string Label { get; }

        public string Tooltip { get; }
    }
}
