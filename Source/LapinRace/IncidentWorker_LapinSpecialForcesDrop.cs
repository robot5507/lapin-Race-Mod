using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace LapinRace
{
    public class IncidentWorker_LapinSpecialForcesDrop : IncidentWorker
    {
        private const float MinPoints = 900f;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;
            if (parms.points < MinPoints) return false;

            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("LapinEmpire");
           

            Faction faction = factionDef == null ? null : Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null) return false;

            // 🔥 핵심 추가
            if (!faction.HostileTo(Faction.OfPlayer))
                return false;

            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null) return false;

            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("LapinEmpire");
            Faction faction = factionDef == null ? null : Find.FactionManager.FirstFactionOfDef(factionDef);
            if (faction == null) return false;

            IntVec3 center = map.Center;
            CellFinder.TryFindRandomCellNear(center, map, 12, c => c.Standable(map) && !c.Fogged(map), out IntVec3 dropCell);

            if (!dropCell.IsValid)
                dropCell = center;

            List<Pawn> pawns = new List<Pawn>();

            int count = GenMath.RoundRandom(parms.points / 350f);
            count = count < 2 ? 2 : count;
            count = count > 6 ? 6 : count;

            PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinMaidSpecialForces");
            if (kindDef == null)
            {
                Log.Warning("[LapinRace] PawnKindDef LapinMaidSpecialForces not found.");
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                Pawn pawn = PawnGenerator.GeneratePawn(kindDef, faction);

                LapinSpecialForcesLoadoutUtility.GiveSpecialForcesLoadout(pawn);

                pawns.Add(pawn);
            }

            DropPodUtility.DropThingsNear(dropCell, map, pawns, 110, false, false, true);

            Lord lord = LordMaker.MakeNewLord(
    faction,
    new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false),
    map,
    pawns
);

            foreach (Pawn pawn in pawns)
            {
                if (pawn.Spawned)
                {
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony);
                    pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced);
                }
            }

            SendStandardLetter(parms, pawns);

            return true;
        }
    }
}