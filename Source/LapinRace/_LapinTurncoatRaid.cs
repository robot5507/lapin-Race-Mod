using RimWorld;
using Verse;

namespace LapinRace
{
    public class IncidentWorker_LapinTurncoatRaid : IncidentWorker_RaidEnemy
    {
        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("Lapin_Turncoat");
            if (factionDef == null)
            {
                Log.Warning("[LapinRace] FactionDef Lapin_Turncoat not found.");
                return false;
            }

            Faction faction = Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null)
            {
                Log.Warning("[LapinRace] No active faction found for Lapin_Turncoat.");
                return false;
            }

            parms.faction = faction;

            return base.TryExecuteWorker(parms);
        }
    }
}