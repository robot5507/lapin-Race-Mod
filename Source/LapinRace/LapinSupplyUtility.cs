using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI.Group;

namespace LapinRace
{
    public static class LapinSupplyUtility
    {
        public const int RequiredAmount = 10;
        public const int SilverReward = 200;
        public const int GoodwillReward = 12;

        public static ThingDef RandomRequestedSupply()
        {
            List<ThingDef> candidates = new List<ThingDef>
            {
                DefDatabase<ThingDef>.GetNamedSilentFail("LP_Butter"),
                DefDatabase<ThingDef>.GetNamedSilentFail("LP_StrawberryWine"),
                DefDatabase<ThingDef>.GetNamedSilentFail("LP_Campague")
            };

            candidates.RemoveAll(t => t == null);

            if (candidates.Count == 0)
            {
                Log.Error("[LapinRace] LP_Butter, LP_StrawberryWine, LP_Campague 중 유효한 ThingDef를 찾지 못했습니다.");
                return ThingDefOf.MealSimple;
            }

            return candidates.RandomElement();
        }

        public static ThingDef GetRequestedSupplyFromRepresentative(Pawn representative)
        {
            Lord lord = representative?.GetLord();

            if (lord?.LordJob is LordJob_LapinWaitForSupplies lapinJob &&
                lapinJob.requestedThingDef != null)
            {
                return lapinJob.requestedThingDef;
            }

            return ThingDefOf.MealSimple;
        }

        public static void GiveSupplyReward(Pawn representative)
        {
            if (representative == null || representative.Map == null)
            {
                return;
            }

            Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
            silver.stackCount = SilverReward;

            GenPlace.TryPlaceThing(
                silver,
                representative.Position,
                representative.Map,
                ThingPlaceMode.Near
            );

            Faction faction = representative.Faction;

            if (faction != null &&
                faction != Faction.OfPlayer &&
                !faction.HostileTo(Faction.OfPlayer))
            {
                faction.TryAffectGoodwillWith(Faction.OfPlayer, GoodwillReward);
            }
        }
    }
}