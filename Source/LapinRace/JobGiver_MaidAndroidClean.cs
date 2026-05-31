using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_MaidAndroidClean : ThinkNode_JobGiver
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

            CompPrototypeAndroidFuel fuelComp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
            if (fuelComp != null && !fuelComp.HasEnergy)
            {
                return null;
            }

            WorkGiverDef cleanDef = DefDatabase<WorkGiverDef>.GetNamedSilentFail("CleanFilth");
            if (cleanDef == null || cleanDef.Worker == null)
            {
                return null;
            }

            WorkGiver_Scanner scanner = cleanDef.Worker as WorkGiver_Scanner;
            if (scanner == null)
            {
                return null;
            }

            Job bestJob = null;
            float bestDistSq = float.MaxValue;

            IEnumerable<Thing> things = scanner.PotentialWorkThingsGlobal(pawn);
            if (things != null)
            {
                foreach (Thing thing in things)
                {
                    if (thing == null || thing.Destroyed || !thing.Spawned)
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

                    if (!scanner.HasJobOnThing(pawn, thing, false))
                    {
                        continue;
                    }

                    Job job = scanner.JobOnThing(pawn, thing, false);
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
            }

            IEnumerable<IntVec3> cells = scanner.PotentialWorkCellsGlobal(pawn);
            if (cells != null)
            {
                foreach (IntVec3 cell in cells)
                {
                    if (!cell.InBounds(pawn.Map))
                    {
                        continue;
                    }

                    if (!IsAllowedByArea(pawn, cell))
                    {
                        continue;
                    }

                    if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                    {
                        continue;
                    }

                    if (!scanner.HasJobOnCell(pawn, cell, false))
                    {
                        continue;
                    }

                    Job job = scanner.JobOnCell(pawn, cell, false);
                    if (job == null)
                    {
                        continue;
                    }

                    float distSq = pawn.Position.DistanceToSquared(cell);
                    if (bestJob == null || distSq < bestDistSq)
                    {
                        bestJob = job;
                        bestDistSq = distSq;
                    }
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