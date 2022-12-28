using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    public class StuffComparerBasic : StuffComparer {
        private readonly HashSet<StatDef> stuffBases;
        private readonly HashSet<StatDef> stuffProps;

        public StuffComparerBasic(ThingDef thing,
                                  IEnumerable<StatDef> stats,
                                  HashSet<StatDef> stuffBases,
                                  HashSet<StatDef> stuffProps)
            : base(thing, stats) {
            this.stuffBases = stuffBases;
            this.stuffProps = stuffProps;
        }

        protected override IEnumerable<Generator> ExtraGenerators() {
            yield return new DefaultGenerator(this);
        }

        private static readonly IEnumerable<StatModifier> empty = 
            Enumerable.Empty<StatModifier>();
        private static readonly StatRequest emptyReq = StatRequest.ForEmpty();

        public bool IsDefault => cmp == DEFAULT;

        public void AugmentIngredientDescription(ThingDef stuff, ref string description) {
            var stuffReq = StatRequest.For(stuff, null);
            var entires = 
                Concat(
                    From(stuff.statBases, stuffBases),
                    From(stuff.stuffProps?.statFactors, stuffProps, false),
                    From(stuff.stuffProps?.statOffsets, stuffProps, true))
                .Select(Entry)
                .Where(e => e.ShouldDisplay)
                .ToList();
            entires.Sort(CompareEntries);

            var tabLen = Text.CalcSize("\t").x;
            if (entires.Count > 0) {
                var labels = new List<string>();
                var values = new Dictionary<string, string>();
                foreach (var entry in entires) {
                    string label = entry.LabelCap;
                    string value = entry.ValueString;
                    if (values.ContainsKey(label)) {
                        values[label] += ", " + value;
                    } else {
                        labels.Add(label);
                        values.Add(label, value);
                    }
                }

                int maxTab = labels.Max(Tabs) + 1;
                var buf = new StringBuilder(description);
                buf.Append('\n');
                foreach (var label in labels) {
                    buf.Append('\n');
                    buf.Append(label);
                    buf.Append('\t', maxTab - Tabs(label));
                    buf.Append(values[label]);
                }
                description = buf.ToString();
            }

            int Tabs(string s) => (int) (Text.CalcSize(s).x / tabLen);

            IEnumerable<T> Concat<T>(params IEnumerable<T>[] args) => args.SelectMany(a => a);

            int CompareEntries(StatDrawEntry a, StatDrawEntry b) {
                //sd.category.displayOrder, sd.DisplayPriorityWithinCategory descending, sd.LabelCap
                int res = a.category.displayOrder.CompareTo(b.category.displayOrder);
                if (res != 0) return res;
                res = b.DisplayPriorityWithinCategory.CompareTo(a.DisplayPriorityWithinCategory);
                if (res != 0) return res;
                return a.LabelCap.CompareTo(b.LabelCap);
            }

            IEnumerable<(StatModifier mod, bool? offset)> From(
                List<StatModifier> mods, HashSet<StatDef> limitTo, bool? offset = null)
                => (mods ?? empty)
                    .Where(mod => limitTo.Contains(mod.stat))
                    .Select(mod => (mod, offset));

            StatDrawEntry Entry((StatModifier mod, bool? offset) p)
                => new StatDrawEntry(Cat(p.mod.stat, p.offset),
                                     p.mod.stat,
                                     p.mod.value,
                                     p.offset.HasValue ? emptyReq : stuffReq,
                                     Sense(p.offset));

            StatCategoryDef Cat(StatDef stat, bool? offset) 
                => offset.HasValue 
                    ? offset.Value
                        ? StatCategoryDefOf.StuffStatOffsets 
                        : StatCategoryDefOf.StuffStatFactors
                    : stat.category;

            ToStringNumberSense Sense(bool? offset) 
                => offset.HasValue 
                    ? offset.Value
                        ? ToStringNumberSense.Offset
                        : ToStringNumberSense.Factor
                    : ToStringNumberSense.Undefined;
        }

        private static readonly DefaultCompare DEFAULT = new DefaultCompare();

        private class DefaultCompare : ICompareFor {
            public string Label => Strings.SortDefaultButtonLabel;

            public string Tooltip => Strings.SortDefaultButtonTip;

            public int Compare(ThingDef thing, ThingDef stuffX, ThingDef stuffY) => 0;

            public ICompareFor Copy() => this;
        }

        private class DefaultGenerator : Generator {
            public DefaultGenerator(StuffComparerBasic parent) 
                : base(parent, null) {}

            public override string Label => Strings.SortDefaultLabel;

            public override ICompareFor Create() => DEFAULT;

            public override void MouseOver(Rect r) 
                => TooltipHandler.TipRegion(r, () => Strings.SortDefaultTip, 73256489);
        }
    }
}
