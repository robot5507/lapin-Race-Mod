using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class JobGiver_AutomatonStandby : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            if (pawn == null || pawn.Dead)
            {
                return null;
            }

            // 소집 중에는 여기서 아무 Job도 주지 않는다.
            // 소집 중 명령은 위쪽의 JobGiver_Orders / ThinkNode_QueuedJob가 처리한다.
            if (pawn.Drafted)
            {
                return null;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Wait);
            job.expiryInterval = 120;
            return job;
        }
    }
}
