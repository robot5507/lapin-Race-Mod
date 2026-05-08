using RimWorld;
using Verse;

namespace LapinRace
{
    public class IncidentWorker_LapinFollowUpTroops : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            IncidentDef raidDef = IncidentDefOf.RaidEnemy;
            if (raidDef == null) return false;

            IncidentParms newParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            newParms.faction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("LapinEmpire"));
            newParms.points = parms.points * 0.6f;

            bool result = raidDef.Worker.TryExecute(newParms);
            if (!result) return false;

            Find.LetterStack.ReceiveLetter(
                 "라핀 증원 도착",
             "첫 공격 이후 라핀 증원군이 전장에 도착했습니다.",
             LetterDefOf.ThreatBig,
             new TargetInfo(map.Center, map)
             );

            return true;
        }
    }
}