using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_PrototypeAndroidSelfRefuel : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead || pawn.Map == null)
            {
                return null;
            }

            if (CompPrototypeAndroidWorkControl.IsInStandby(pawn))
            {
                return null;
            }

            CompPrototypeAndroidFuel comp = pawn.TryGetComp<CompPrototypeAndroidFuel>();
            if (comp == null)
            {
                return null;
            }

            if (!comp.NeedsAutoRefuel)
            {
                return null;
            }

            Thing fuel = comp.FindReachableFuelFor(pawn);
            if (fuel == null)
            {
                return null;
            }

            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_PrototypeAndroidSelfRefuel");
            if (jobDef == null)
            {
                Log.Error("[LapinRace] LP_PrototypeAndroidSelfRefuel JobDef를 찾을 수 없습니다.");
                return null;
            }

            int count = comp.NeededFuelCount;
            if (count < 1)
            {
                count = 1;
            }

            if (count > fuel.stackCount)
            {
                count = fuel.stackCount;
            }

            Job job = JobMaker.MakeJob(jobDef, fuel);
            job.count = count;
            return job;
        }
    }
}