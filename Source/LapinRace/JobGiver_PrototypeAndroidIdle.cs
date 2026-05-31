using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_PrototypeAndroidIdle : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed || pawn.Map == null)
            {
                return null;
            }

            if (CompPrototypeAndroidWorkControl.IsInStandby(pawn))
            {
                Job waitJob = JobMaker.MakeJob(JobDefOf.Wait);
                waitJob.expiryInterval = 600;
                waitJob.locomotionUrgency = LocomotionUrgency.None;
                return waitJob;
            }

            IntVec3 wanderDest;

            if (!TryFindWanderCellInAllowedArea(pawn, out wanderDest))
            {
                Job waitJob = JobMaker.MakeJob(JobDefOf.Wait);
                waitJob.expiryInterval = 120;
                waitJob.locomotionUrgency = LocomotionUrgency.None;
                return waitJob;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, wanderDest);
            job.expiryInterval = 400;
            job.locomotionUrgency = LocomotionUrgency.Amble;

            return job;
        }

        private static bool TryFindWanderCellInAllowedArea(Pawn pawn, out IntVec3 result)
        {
            result = IntVec3.Invalid;

            if (pawn == null || pawn.Map == null)
            {
                return false;
            }

            Area allowedArea = null;

            if (pawn.playerSettings != null)
            {
                allowedArea = pawn.playerSettings.AreaRestrictionInPawnCurrentMap;
            }

            for (int i = 0; i < 40; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(
                    pawn.Position,
                    pawn.Map,
                    8
                );

                if (!cell.InBounds(pawn.Map))
                {
                    continue;
                }

                if (allowedArea != null && !allowedArea[cell])
                {
                    continue;
                }

                if (!cell.Standable(pawn.Map))
                {
                    continue;
                }

                if (cell.IsForbidden(pawn))
                {
                    continue;
                }

                if (cell.GetDangerFor(pawn, pawn.Map) > Danger.Some)
                {
                    continue;
                }

                if (!pawn.CanReach(cell, PathEndMode.OnCell, Danger.Some))
                {
                    continue;
                }

                result = cell;
                return true;
            }

            return false;
        }
    }
}
