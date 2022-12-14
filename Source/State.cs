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
            if (create) return states.GetValue(bill, b => new State(b));
            if (states.TryGetValue(bill, out State state)) return state;
            return null;
        }

        private bool active = false;
        private readonly StuffComparer comparer;

        private State(Bill bill) 
            => comparer = StuffComparer.For(bill.recipe?.ProducedThingDef);

        public bool Active => active;

        public bool Valid => comparer != null;

        public StuffComparer Comparer => comparer.NoCell();

        public StuffComparer ComparerFor(IntVec3 cell) => comparer.ForCell(cell);

        public const float GUIGap    = 4f;
        public const float GUIHeight = Widgets.CheckboxSize;
        public const float GUISpace  = GUIHeight + 3 * GUIGap;
        public const string CheckLabel = "Use best by:";

        public void DoGUI(Rect rect) {
            if (Valid) {
                Text.Anchor = TextAnchor.MiddleLeft;
                var label = rect.LeftPartPixels(Text.CalcSize(CheckLabel).x);
                Widgets.Label(label, CheckLabel);
                rect.xMin += label.width + 2 * GUIGap;
                rect.xMax -= Widgets.CheckboxSize + 2 * GUIGap;
                comparer.DoButton(rect);
                Widgets.Checkbox(rect.xMax + 2 * GUIGap, rect.y, ref active);
                GenUI.ResetLabelAlign();
            }
        }

        public void ExposeData() {
            if (comparer != null && Scribe.EnterNode(Strings.ID)) {
                Scribe_Values.Look(ref active, "active");
                comparer.ExposeData();
                Scribe.ExitNode();
            }
        }
    }
}
