using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    public class StuffComparer : IComparer<Thing>, IComparer<ThingDef>, IExposable {
        private ICompareFor cmp;
        private IntVec3 origin;
        private bool hasOrigin = false;
        private readonly ThingDef thing;
        private readonly List<StatDef> stats;
        private readonly List<Generator> generators;

        public static StuffComparer For(ThingDef thing) {
            if (thing == null || !thing.MadeFromStuff) return null;
            var stuffs = GenStuff.AllowedStuffsFor(thing);
            var stuffBases = stuffs
                .SelectMany(s => s.statBases)
                .Select(sm => sm.stat)
                .ToHashSet();
            var stuffStats = stuffs
                .SelectMany(s => Of(s.stuffProps?.statFactors, s.stuffProps?.statOffsets))
                .SelectMany(l => l.Select(m => m.stat))
                .ToHashSet();

            var req = StatRequest.For(thing, null);
            var thingStats = DefDatabase<StatDef>.AllDefs
                .Where(s => s.parts != null && s.Worker.ShouldShowFor(req))
                .Select(s => (s, sps: s.GetStatPart<StatPart_Stuff>()))
                .Where(p => (p.sps != null && InStuff(p.sps)) || stuffStats.Contains(p.s))
                .Select(s => s.s)
                .ToList();

            if (thingStats.Count == 0) return null;
            thingStats.SortBy(s => s.label);

            if (thing.IsMeleeWeapon) {
                thingStats.Insert(0, StatDefOf.MeleeWeapon_AverageDPS);
                thingStats.Insert(1, StatDefOf.MeleeWeapon_AverageArmorPenetration);
            }

            return new StuffComparer(thing, thingStats);

            bool InStuff(StatPart_Stuff sps)
                => stuffBases.Contains(sps.stuffPowerStat) 
                || stuffBases.Contains(sps.multiplierStat);

            IEnumerable<T> Of<T>(params T[] elems) => elems.Where(e => e != null);
        }

        private StuffComparer(ThingDef thing, List<StatDef> stats) {
            this.thing = thing;
            this.stats = stats;
            generators = Generators().ToList();
            cmp = generators[0].Create();
        }

        public void DoButton(Rect rect) {
            var labelRect = rect;
            if (generators.Count > 1) {
                if (Widgets.ButtonText(rect, "")) Menu();
                labelRect = rect.ContractedBy(5f, 2f);
            }
            Text.WordWrap = false;
            Widgets.Label(labelRect, cmp.Label);
            TooltipHandler.TipRegion(rect, cmp.Tooltip);
            Text.WordWrap = true;
        }

        private void Menu() {
            var menu = generators.Select(g => g.Option());
            Find.WindowStack.Add(new FloatMenu(menu.ToList()));
        }

        private IEnumerable<Generator> Generators() {
            StatDef first = null;
            if (thing.IsMeleeWeapon) {
                first = StatDefOf.MeleeWeapon_AverageDPS;
                yield return new Generator(this, first);
                yield return new WeightedGenerator(this, "Attack strength", Attack);
            } else if (thing.IsApparel) {
                yield return new WeightedGenerator(this, "Armor", Armor);
                yield return new WeightedGenerator(this, "Insulation", Insulation);
            }

            foreach (var stat in stats) {
                if (stat != first) {
                    yield return new Generator(this, stat);
                }
            }
        }

        public StuffComparer ForCell(IntVec3 cell) {
            origin = cell;
            hasOrigin = true;
            return this;
        }

        public StuffComparer NoCell() {
            hasOrigin = false;
            return this;
        }

        public int Compare(Thing x, Thing y) {
            int res = cmp.Compare(thing, x.def, y.def);
            if (res == 0 && hasOrigin) {
                res = Dist(x) - Dist(y);
            }
            return res;

            int Dist(Thing x) => (x.PositionHeld - origin).LengthHorizontalSquared;
        }

        public int Compare(ThingDef x, ThingDef y) => cmp.Compare(thing, x, y);

        public void ExposeData() => Scribe_Deep.Look(ref cmp, "compare");

        private class Generator {
            private readonly StuffComparer parent;
            public readonly StatDef stat;

            public virtual ICompareFor Create() => new CompareSingle(stat);

            public virtual void MenuAction() => parent.cmp = Create();

            public virtual void MouseOver(Rect r) {}

            public virtual string Label => stat.LabelCap;

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

        private class WeightedGenerator : Generator {
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
        }
    }
}
