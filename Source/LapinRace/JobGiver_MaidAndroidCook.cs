using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_MaidAndroidCook : ThinkNode_JobGiver
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

            List<WorkGiver> cookingWorkGivers = GetCookingWorkGivers();
            if (cookingWorkGivers == null || cookingWorkGivers.Count == 0)
            {
                Log.Warning("[LapinRace] Cooking WorkGiver를 찾지 못했습니다.");
                return null;
            }

            for (int i = 0; i < cookingWorkGivers.Count; i++)
            {
                WorkGiver workGiver = cookingWorkGivers[i];

                if (workGiver == null || workGiver.def == null)
                {
                    continue;
                }

                if (!PawnCanUseWorkGiver(pawn, workGiver))
                {
                    continue;
                }

                Job nonScanJob = null;

                try
                {
                    nonScanJob = workGiver.NonScanJob(pawn);
                }
                catch
                {
                    nonScanJob = null;
                }

                if (nonScanJob != null)
                {
                    nonScanJob.workGiverDef = workGiver.def;
                    return nonScanJob;
                }

                WorkGiver_Scanner scanner = workGiver as WorkGiver_Scanner;
                if (scanner == null)
                {
                    continue;
                }

                Job job = TryGetJobFromScanner(pawn, scanner);
                if (job != null)
                {
                    job.workGiverDef = scanner.def;
                    return job;
                }
            }

            return null;
        }

        private static List<WorkGiver> GetCookingWorkGivers()
        {
            WorkTypeDef cooking = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
            if (cooking == null)
            {
                Log.Warning("[LapinRace] Cooking WorkTypeDef를 찾지 못했습니다.");
                return null;
            }

            List<WorkGiverDef> defs = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Where(def => def != null && def.workType == cooking && def.Worker != null)
                .OrderByDescending(def => def.priorityInType)
                .ToList();

            List<WorkGiver> result = new List<WorkGiver>();

            for (int i = 0; i < defs.Count; i++)
            {
                result.Add(defs[i].Worker);
            }

            return result;
        }

        private static Job TryGetJobFromScanner(Pawn pawn, WorkGiver_Scanner scanner)
        {
            if (pawn == null || scanner == null)
            {
                return null;
            }

            if (scanner.def.scanThings)
            {
                Job thingJob = TryGetThingJob(pawn, scanner);
                if (thingJob != null)
                {
                    return thingJob;
                }
            }

            if (scanner.def.scanCells)
            {
                Job cellJob = TryGetCellJob(pawn, scanner);
                if (cellJob != null)
                {
                    return cellJob;
                }
            }

            return null;
        }

        private static Job TryGetThingJob(Pawn pawn, WorkGiver_Scanner scanner)
        {
            IEnumerable<Thing> potentialThings = scanner.PotentialWorkThingsGlobal(pawn);

            if (potentialThings == null)
            {
                ThingRequest request = scanner.PotentialWorkThingRequest;
                potentialThings = pawn.Map.listerThings.ThingsMatching(request);
            }

            if (potentialThings == null)
            {
                return null;
            }

            Thing bestThing = null;
            float bestDistSq = float.MaxValue;
            float bestPriority = float.MinValue;

            foreach (Thing thing in potentialThings)
            {
                if (thing == null || thing.Destroyed || !thing.Spawned)
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

                if (!scanner.AllowUnreachable)
                {
                    if (!pawn.CanReserveAndReach(thing, scanner.PathEndMode, scanner.MaxPathDanger(pawn)))
                    {
                        continue;
                    }
                }

                bool hasJob = false;

                try
                {
                    hasJob = scanner.HasJobOnThing(pawn, thing, false);
                }
                catch
                {
                    hasJob = false;
                }

                if (!hasJob)
                {
                    continue;
                }

                float distSq = pawn.Position.DistanceToSquared(thing.Position);
                float priority = scanner.Prioritized ? scanner.GetPriority(pawn, thing) : 0f;

                if (bestThing == null)
                {
                    bestThing = thing;
                    bestDistSq = distSq;
                    bestPriority = priority;
                    continue;
                }

                if (scanner.Prioritized)
                {
                    if (priority > bestPriority || (priority == bestPriority && distSq < bestDistSq))
                    {
                        bestThing = thing;
                        bestDistSq = distSq;
                        bestPriority = priority;
                    }
                }
                else
                {
                    if (distSq < bestDistSq)
                    {
                        bestThing = thing;
                        bestDistSq = distSq;
                    }
                }
            }

            if (bestThing == null)
            {
                return null;
            }

            try
            {
                return scanner.JobOnThing(pawn, bestThing, false);
            }
            catch
            {
                return null;
            }
        }

        private static Job TryGetCellJob(Pawn pawn, WorkGiver_Scanner scanner)
        {
            IEnumerable<IntVec3> cells = scanner.PotentialWorkCellsGlobal(pawn);
            if (cells == null)
            {
                return null;
            }

            IntVec3 bestCell = IntVec3.Invalid;
            float bestDistSq = float.MaxValue;
            float bestPriority = float.MinValue;

            int checkedCount = 0;

            foreach (IntVec3 cell in cells)
            {
                checkedCount++;
                if (checkedCount > 300)
                {
                    break;
                }

                if (!cell.InBounds(pawn.Map))
                {
                    continue;
                }

                if (!IsAllowedByArea(pawn, cell))
                {
                    continue;
                }

                if (cell.IsForbidden(pawn))
                {
                    continue;
                }

                if (!scanner.AllowUnreachable)
                {
                    if (!pawn.CanReach(cell, scanner.PathEndMode, scanner.MaxPathDanger(pawn)))
                    {
                        continue;
                    }
                }

                bool hasJob = false;

                try
                {
                    hasJob = scanner.HasJobOnCell(pawn, cell, false);
                }
                catch
                {
                    hasJob = false;
                }

                if (!hasJob)
                {
                    continue;
                }

                float distSq = pawn.Position.DistanceToSquared(cell);
                float priority = scanner.Prioritized ? scanner.GetPriority(pawn, cell) : 0f;

                if (!bestCell.IsValid)
                {
                    bestCell = cell;
                    bestDistSq = distSq;
                    bestPriority = priority;
                    continue;
                }

                if (scanner.Prioritized)
                {
                    if (priority > bestPriority || (priority == bestPriority && distSq < bestDistSq))
                    {
                        bestCell = cell;
                        bestDistSq = distSq;
                        bestPriority = priority;
                    }
                }
                else
                {
                    if (distSq < bestDistSq)
                    {
                        bestCell = cell;
                        bestDistSq = distSq;
                    }
                }
            }

            if (!bestCell.IsValid)
            {
                return null;
            }

            try
            {
                return scanner.JobOnCell(pawn, bestCell, false);
            }
            catch
            {
                return null;
            }
        }

        private static bool PawnCanUseWorkGiver(Pawn pawn, WorkGiver giver)
        {
            if (pawn == null || giver == null)
            {
                return false;
            }

            if (pawn.Destroyed || !pawn.Spawned)
            {
                return false;
            }

            try
            {
                if (giver.MissingRequiredCapacity(pawn) != null)
                {
                    return false;
                }

                if (giver.ShouldSkip(pawn, false))
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }

            return true;
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