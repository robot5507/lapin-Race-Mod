using RimWorld;
using Verse;

namespace LapinRace
{
    public class IncidentWorker_LapinRaid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            Faction lapinFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("LapinEmpire"));
            if (lapinFaction == null) return false;

            if (!lapinFaction.HostileTo(Faction.OfPlayer))
                return false;

            IncidentDef raidDef = IncidentDefOf.RaidEnemy;
            if (raidDef == null) return false;

            IncidentParms newParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            newParms.faction = lapinFaction;
            newParms.points = parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);

            bool result = raidDef.Worker.TryExecute(newParms);
            if (!result) return false;

            QueueFollowUp(map, newParms);
            return true;
        }

        private void QueueFollowUp(Map map, IncidentParms parms)
        {
            IncidentDef followUp = DefDatabase<IncidentDef>.GetNamedSilentFail("LapinFollowUpTroops");
            if (followUp == null) return;

            int delayTicks = Rand.RangeInclusive(30000, 90000);

            Find.Storyteller.incidentQueue.Add(
                new QueuedIncident(
                    new FiringIncident(followUp, null, parms),
                    Find.TickManager.TicksGame + delayTicks
                )
            );
        }
    }
}
