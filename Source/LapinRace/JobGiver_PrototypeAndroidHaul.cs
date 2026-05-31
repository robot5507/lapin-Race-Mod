using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_PrototypeAndroidHaul : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Map == null)
            {
                return null;
            }

            if (CompPrototypeAndroidWorkControl.IsInStandby(pawn))
            {
                return null;
            }

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
            {
                return null;
            }

            CompPrototypeAndroidFuel fuelComp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
            if (fuelComp != null && !fuelComp.HasEnergy)
            {
                return null;
            }

            ICollection<Thing> haulables = pawn.Map.listerHaulables.ThingsPotentiallyNeedingHauling();

            if (haulables == null || haulables.Count == 0)
            {
                return null;
            }

            Job bestJob = null;
            float bestDistSq = float.MaxValue;

            foreach (Thing thing in haulables)
            {
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                if (thing.def == null)
                {
                    continue;
                }

                if (!thing.Spawned)
                {
                    continue;
                }

                if (thing.IsForbidden(pawn))
                {
                    continue;
                }

                if (!IsAllowedByArea(pawn, thing.Position))
                {
                    continue;
                }

                if (!pawn.CanReserveAndReach(thing, PathEndMode.ClosestTouch, Danger.Some))
                {
                    continue;
                }

                IntVec3 storeCell;
                if (!StoreUtility.TryFindBestBetterStoreCellFor(
                    thing,
                    pawn,
                    pawn.Map,
                    StoragePriority.Unstored,
                    pawn.Faction,
                    out storeCell,
                    true
                ))
                {
                    continue;
                }

                if (!IsAllowedByArea(pawn, storeCell))
                {
                    continue;
                }

                Job job = HaulAIUtility.HaulToStorageJob(pawn, thing, false);
                if (job == null)
                {
                    continue;
                }

                float distSq = pawn.Position.DistanceToSquared(thing.Position);

                if (bestJob == null || distSq < bestDistSq)
                {
                    bestJob = job;
                    bestDistSq = distSq;
                }
            }

            return bestJob;
        }

        private static bool IsAllowedByArea(Pawn pawn, IntVec3 cell)
        {
            if (pawn == null || pawn.Map == null)
            {
                return true;
            }

            if (!cell.InBounds(pawn.Map))
            {
                return false;
            }

            if (pawn.playerSettings == null)
            {
                return true;
            }

            Area area = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;

            if (area == null)
            {
                return true;
            }

            return area[cell];
        }
    }
}
