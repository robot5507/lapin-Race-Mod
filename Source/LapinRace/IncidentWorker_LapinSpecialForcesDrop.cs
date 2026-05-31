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
            if (map == null)
            {
                return false;
            }

            if (parms.points < MinPoints)
            {
                return false;
            }

            Faction faction = GetLapinFaction();
            if (faction == null)
            {
                return false;
            }

            if (Faction.OfPlayer == null || !faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            return base.CanFireNowSub(parms);
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;
            if (map == null)
            {
                return false;
            }

            Faction faction = GetLapinFaction();
            if (faction == null)
            {
                return false;
            }

            // CanFireNowSub에서 이미 검사하지만, 직접 실행/큐 실행 대비로 한 번 더 검사
            if (Faction.OfPlayer == null || !faction.HostileTo(Faction.OfPlayer))
            {
                return false;
            }

            if (parms.points < MinPoints)
            {
                return false;
            }

            IntVec3 dropCell;
            if (!TryFindDropCell(map, out dropCell))
            {
                return false;
            }

            List<Pawn> pawns = new List<Pawn>();

            int count = GenMath.RoundRandom(parms.points / 350f);
            count = count < 2 ? 2 : count;
            count = count > 6 ? 6 : count;

            PawnKindDef normalKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinMaidSpecialForces");
            PawnKindDef millitarKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinMaidSpecialForcesMillitar");

            if (normalKind == null || millitarKind == null)
            {
                Log.Warning("[LapinRace] Special forces PawnKindDef not found.");
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                PawnKindDef selectedKind = Rand.Chance(0.5f) ? normalKind : millitarKind;

                Pawn pawn = PawnGenerator.GeneratePawn(selectedKind, faction);

                if (selectedKind == millitarKind)
                {
                    LapinSpecialForcesMillitarLoadoutUtility.GiveSpecialForcesMillitarLoadout(pawn);
                }
                else
                {
                    LapinSpecialForcesLoadoutUtility.GiveSpecialForcesLoadout(pawn);
                }

                pawns.Add(pawn);
            }

            DropPodUtility.DropThingsNear(dropCell, map, pawns, 110, false, false, true);

            LordMaker.MakeNewLord(
                faction,
                new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false),
                map,
                pawns
            );

            SendStandardLetter(parms, pawns);

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

        private static bool TryFindDropCell(Map map, out IntVec3 dropCell)
        {
            IntVec3 center = map.Center;

            if (CellFinder.TryFindRandomCellNear(
                center,
                map,
                12,
                c => c.Standable(map) && !c.Fogged(map),
                out dropCell))
            {
                return true;
            }

            if (center.InBounds(map) && center.Standable(map) && !center.Fogged(map))
            {
                dropCell = center;
                return true;
            }

            return CellFinder.TryFindRandomCell(
                map,
                c => c.Standable(map) && !c.Fogged(map),
                out dropCell
            );
        }
    }
}