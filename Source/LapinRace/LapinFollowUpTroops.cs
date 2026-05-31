using RimWorld;
using Verse;

namespace LapinRace
{
    public class IncidentWorker_LapinFollowUpTroops : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            Faction lapinFaction = GetLapinFaction();
            if (lapinFaction == null)
            {
                return false;
            }

            if (Faction.OfPlayer == null || !lapinFaction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            Faction lapinFaction = GetLapinFaction();
            if (lapinFaction == null)
            {
                return false;
            }

            // 편지/알림을 띄우기 전에 반드시 적대 여부 확인
            if (Faction.OfPlayer == null || !lapinFaction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            IncidentDef raidDef = IncidentDefOf.RaidEnemy;
            if (raidDef == null)
            {
                return false;
            }

            float points = parms.points > 0f
                ? parms.points * 0.6f
                : StorytellerUtility.DefaultThreatPointsNow(map) * 0.6f;

            IncidentParms newParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            newParms.target = map;
            newParms.faction = lapinFaction;
            newParms.points = points;

            bool result = raidDef.Worker.TryExecute(newParms);
            if (!result)
            {
                return false;
            }

            Find.LetterStack.ReceiveLetter(
                "라핀 증원 도착",
                "첫 공격 이후 라핀 증원군이 전장에 도착했습니다.",
                LetterDefOf.ThreatBig,
                new TargetInfo(map.Center, map)
            );

            return true;
        }

        private static Faction GetLapinFaction()
        {
            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("LapinEmpire");
            if (factionDef == null)
            {
                return null;
            }

            return Find.FactionManager.FirstFactionOfDef(factionDef);
        }
    }
}