using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace UseBestMaterials {
    public static class StuffComparers {
        private static readonly HashSet<StatCategoryDef> stuffCats =
            new HashSet<StatCategoryDef> {
                StatCategoryDefOf.StuffStatFactors,
                StatCategoryDefOf.StuffStatOffsets,
            };

        public static (StuffComparerForThing, StuffComparerBasic) For(ThingDef thing) {
            if (thing == null || !thing.MadeFromStuff) return (null, null);
            var req = StatRequest.For(thing, null);

            var stuffs = GenStuff.AllowedStuffsFor(thing);
            var stuffBases = stuffs
                .SelectMany(s => s.statBases)
                .Select(sm => sm.stat)
                .Where(s => stuffCats.Contains(s.category))
                .ToHashSet();
            var stuffProps = stuffs
                .SelectMany(StuffProps)
                .ToHashSet();

            var thingPairs = DefDatabase<StatDef>.AllDefs
                .Where(s => s.Worker.ShouldShowFor(req))
                .Select(s => (s, sps: s.parts?.OfType<StatPart_Stuff>().FirstOrDefault()))
                .Where(p => (p.sps != null && InStuff(p.sps)) || stuffProps.Contains(p.s))
                .ToList();
            var thingStats = thingPairs
                .Select(s => s.s)
                .ToList();
            stuffBases = thingPairs
                .SelectMany(p => StuffPartStats(p.sps))
                .Where(stuffBases.Contains)
                .ToHashSet();
            stuffProps = thingPairs
                .Select(p => p.s)
                .Where(stuffProps.Contains)
                .ToHashSet();

            if (thingStats.Count == 0) return (null, null);
            thingStats.SortBy(s => s.label);

            if (thing.IsMeleeWeapon) {
                thingStats.Insert(0, StatDefOf.MeleeWeapon_AverageDPS);
                thingStats.Insert(1, StatDefOf.MeleeWeapon_AverageArmorPenetration);
                stuffBases.Add(StatDefOf.SharpDamageMultiplier);
                stuffBases.Add(StatDefOf.BluntDamageMultiplier);
            }

            return (new StuffComparerForThing(thing, thingStats), 
                    new StuffComparerBasic(thing, thingStats, stuffBases, stuffProps));

            bool InStuff(StatPart_Stuff sps)
                => stuffBases.Contains(sps.stuffPowerStat)
                || stuffBases.Contains(sps.multiplierStat);

            IEnumerable<StatDef> StuffProps(ThingDef stuff) {
                var props = stuff.stuffProps;
                var off = props?.statOffsets;
                if (off != null) {
                    foreach (var mod in off) {
                        yield return mod.stat;
                    }
                }

                var fact = props?.statFactors;
                if (fact != null) {
                    foreach (var mod in fact) {
                        if (mod.stat.applyFactorsIfNegative || mod.stat.Worker.GetBaseValueFor(req) > 0f) {
                            yield return mod.stat;
                        }
                    }
                }
            }

            IEnumerable<StatDef> StuffPartStats(StatPart_Stuff p) {
                if (p == null) yield break;
                yield return p.stuffPowerStat;
                yield return p.multiplierStat;
            }
        }
    }
}
