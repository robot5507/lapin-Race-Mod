using RimWorld;
using Verse;

namespace LapinRace
{
    public class IncidentWorker_LapinRaid : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!(parms.target is Map))
            {
                return false;
            }

            Faction lapinFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("LapinEmpire"));
            if (lapinFaction == null)
            {
                return false;
            }

            if (Faction.OfPlayer == null)
            {
                return false;
            }

            return lapinFaction.HostileTo(Faction.OfPlayer);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            Faction lapinFaction = Find.FactionManager.FirstFactionOfDef(FactionDef.Named("LapinEmpire"));
            if (lapinFaction == null)
            {
                return false;
            }

            if (Faction.OfPlayer == null || !lapinFaction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            IncidentDef raidDef = IncidentDefOf.RaidEnemy;
            if (raidDef == null)
            {
                return false;
            }

            IncidentParms newParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            newParms.faction = lapinFaction;
            newParms.points = parms.points > 0f ? parms.points : StorytellerUtility.DefaultThreatPointsNow(map);
            newParms.target = map;

            bool result = raidDef.Worker.TryExecute(newParms);
            if (!result)
            {
                return false;
            }

            QueueFollowUp(map, lapinFaction, newParms.points);
            return true;
        }

        private void QueueFollowUp(Map map, Faction lapinFaction, float points)
        {
            if (map == null || lapinFaction == null)
            {
                return;
            }

            if (Faction.OfPlayer == null || !lapinFaction.HostileTo(Faction.OfPlayer))
            {
                return;
            }

            IncidentDef followUp = DefDatabase<IncidentDef>.GetNamedSilentFail("LapinFollowUpTroops");
            if (followUp == null)
            {
                return;
            }

            IncidentParms followParms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.ThreatBig, map);
            followParms.target = map;
            followParms.faction = lapinFaction;
            followParms.points = points;

            int delayTicks = Rand.RangeInclusive(30000, 90000);

            Find.Storyteller.incidentQueue.Add(
                new QueuedIncident(
                    new FiringIncident(followUp, null, followParms),
                    Find.TickManager.TicksGame + delayTicks
                )
            );
        }
    }
}