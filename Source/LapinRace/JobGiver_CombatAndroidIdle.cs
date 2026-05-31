using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_CombatAndroidIdle : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Downed)
            {
                return null;
            }

            if (pawn.drafter != null && pawn.drafter.Drafted)
            {
                return null;
            }

            CompAutomatonEnergy energy = pawn.TryGetComp<CompAutomatonEnergy>();
            if (energy != null && !energy.HasEnergy)
            {
                Job powerlessWait = JobMaker.MakeJob(JobDefOf.Wait);
                powerlessWait.expiryInterval = 600;
                powerlessWait.locomotionUrgency = LocomotionUrgency.None;
                return powerlessWait;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Wait);
            job.expiryInterval = 600;
            job.locomotionUrgency = LocomotionUrgency.None;

            return job;
        }
    }
}
