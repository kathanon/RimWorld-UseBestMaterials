using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace UseBestMaterials {
    public class State : IExposable {
        private static readonly ConditionalWeakTable<Bill, State> states 
            = new ConditionalWeakTable<Bill, State>();

        public static State For(Bill bill, bool create = false) {
            if (bill == null) return null;
            if (create) return states.GetValue(bill, b => new State(b));
            if (states.TryGetValue(bill, out State state)) return state;
            return null;
        }

        private bool active = false;
        private readonly StuffComparerForThing forThing;
        private readonly StuffComparerBasic basic;

        private State(Bill bill) 
            => (forThing, basic) = StuffComparers.For(bill.recipe?.ProducedThingDef);

        public bool Active => active;

        public bool Valid => forThing != null;

        public void CopyFrom(Bill bill) {
            var from = For(bill);
            if (from != null && from.Valid && Valid) {
                active = from.active;
                forThing.CopyFrom(from.forThing);
            }
        }

        public StuffComparerBasic ForStuff => basic;

        public StuffComparerForThing ForThingFrom(IntVec3 cell) => forThing.ForCell(cell);

        public const float GUIGap    = 4f;
        public const float GUIHeight = Widgets.CheckboxSize;
        public const float GUISpace  = GUIHeight + 3 * GUIGap;

        public void DoGUI(Rect rect) {
            if (Valid) {
                Text.Anchor = TextAnchor.MiddleLeft;
                var label = rect.LeftPartPixels(Text.CalcSize(Strings.UseBestBy).x);
                Widgets.Label(label, Strings.UseBestBy);
                rect.xMin += label.width + 2 * GUIGap;
                rect.xMax -= Widgets.CheckboxSize + 2 * GUIGap;
                forThing.DoButton(rect);
                Widgets.Checkbox(rect.xMax + 2 * GUIGap, rect.y, ref active);
                GenUI.ResetLabelAlign();
            }
        }

        public void DoSortButton(Rect rect) 
            => basic?.DoButton(rect);

        public void AugmentIngredientDescription(ThingDef stuff, ref string description) 
            => basic?.AugmentIngredientDescription(stuff, ref description);

        public void ExposeData() {
            if (forThing != null && Scribe.EnterNode(Strings.ID)) {
                Scribe_Values.Look(ref active, "active");
                forThing.ExposeData();
                Scribe.ExitNode();
            }
        }
    }
}
