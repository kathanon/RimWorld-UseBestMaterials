using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    public abstract class StuffComparer : IComparer<ThingDef> {
        protected ICompareFor cmp;
        protected readonly ThingDef thing;
        protected readonly List<Generator> generators;

        protected StuffComparer(ThingDef thing, IEnumerable<StatDef> stats) {
            this.thing = thing;
            generators = Generators(stats).ToList();
            cmp = generators[0].Create();
        }

        protected virtual IEnumerable<Generator> ExtraGenerators()
            => Enumerable.Empty<Generator>();

        public int Compare(ThingDef x, ThingDef y) => cmp.Compare(thing, x, y);

        public void DoButton(Rect rect) {
            var labelRect = rect;
            bool button = generators.Count > 1;
            if (button) {
                if (Widgets.ButtonText(rect, "")) Menu();
                labelRect = rect.ContractedBy(5f, 2f);
            }
            Text.WordWrap = false;
            string label = cmp.Label;
            Text.Anchor = (!button || Text.CalcSize(label).x > labelRect.width) 
                ? TextAnchor.MiddleLeft 
                : TextAnchor.MiddleCenter;
            Widgets.Label(labelRect, label);
            TooltipHandler.TipRegion(rect, cmp.Tooltip);
            GenUI.ResetLabelAlign();
            Text.WordWrap = true;
        }

        private void Menu() {
            var menu = generators.Select(g => g.Option());
            Find.WindowStack.Add(new FloatMenu(menu.ToList()));
        }

        private IEnumerable<Generator> Generators(IEnumerable<StatDef> stats) {
            foreach (var gen in ExtraGenerators()) {
                yield return gen;
            }

            StatDef first = null;
            if (thing.IsMeleeWeapon) {
                first = StatDefOf.MeleeWeapon_AverageDPS;
                yield return new Generator(this, first);
                yield return new WeightedGenerator(this, Strings.WeightedAttackLabel, Attack);
            } else if (thing.IsApparel) {
                yield return new WeightedGenerator(this, Strings.WeightedArmorLabel, Armor);
                yield return new WeightedGenerator(this, Strings.WeightedInsulationLabel, Insulation);
            }

            foreach (var stat in stats) {
                if (stat != first) {
                    yield return new Generator(this, stat);
                }
            }
        }

        protected class Generator {
            private readonly StuffComparer parent;
            public readonly StatDef stat;

            public virtual ICompareFor Create() => new CompareSingle(stat);

            public virtual void MenuAction() => parent.cmp = Create();

            public virtual void MouseOver(Rect r) {}

            public virtual string Label => stat.LabelCap;

            public virtual bool Matches(ICompareFor cmp) 
                => cmp is CompareSingle cs && cs.Stat == stat;

            public FloatMenuOption Option()
                => new FloatMenuOption(Label, MenuAction, mouseoverGuiAction: MouseOver);

            public Generator(StuffComparer parent, StatDef stat) {
                this.parent = parent;
                this.stat = stat;
            }
        }

        private static readonly (StatDef def, float weight)[] Armor = {
            (StatDefOf.ArmorRating_Sharp,   2f),
            (StatDefOf.ArmorRating_Blunt,   1f),
            (StatDefOf.ArmorRating_Heat,  0.2f),
        };

        private static readonly (StatDef def, float weight)[] Insulation = {
            (StatDefOf.Insulation_Cold, 1f),
            (StatDefOf.Insulation_Heat, 1f),
        };

        private static readonly (StatDef def, float weight)[] Attack = {
            (StatDefOf.MeleeWeapon_AverageDPS,              1f),
            (StatDefOf.MeleeWeapon_AverageArmorPenetration, 4f),
        };

        protected class WeightedGenerator : Generator {
            private readonly ICompareFor sample;
            private readonly string name;
            private readonly (StatDef def, float weight)[] stats;

            public WeightedGenerator(StuffComparer parent, string name, (StatDef def, float weight)[] stats)
                : base(parent, null) {
                this.stats = stats;
                this.name = name;
                sample = Create();
            }

            public override string Label => name;

            public override ICompareFor Create() => new CompareWeighted(stats, name);

            public override void MouseOver(Rect r)
                => TooltipHandler.TipRegion(r, () => sample.Tooltip, 73256488);

            public override bool Matches(ICompareFor cmp)
                => cmp is CompareWeighted cw && cw.Matches(stats);
        }
    }
}

