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

            // 플레이어와 적대 관계일 때만 발생
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

            // 드롭 위치 찾기
            IntVec3 center = map.Center;
            CellFinder.TryFindRandomCellNear(center, map, 12,
                c => c.Standable(map) && !c.Fogged(map),
                out IntVec3 dropCell);

            if (!dropCell.IsValid)
                dropCell = center;

            List<Pawn> pawns = new List<Pawn>();

            // 인원 수 계산
            int count = GenMath.RoundRandom(parms.points / 350f);
            count = count < 2 ? 2 : count;
            count = count > 6 ? 6 : count;

            // PawnKind 가져오기
            PawnKindDef normalKind =
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinMaidSpecialForces");

            PawnKindDef millitarKind =
                DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinMaidSpecialForcesMillitar");

            if (normalKind == null || millitarKind == null)
            {
                Log.Warning("[LapinRace] Special forces PawnKindDef not found.");
                return false;
            }

            // 🔥 핵심: 50:50 랜덤 생성
            for (int i = 0; i < count; i++)
            {
                PawnKindDef selectedKind = Rand.Chance(0.5f) ? normalKind : millitarKind;

                Pawn pawn = PawnGenerator.GeneratePawn(selectedKind, faction);

                // 장비 분기 지급
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

            // 드롭포드 투하
            DropPodUtility.DropThingsNear(dropCell, map, pawns, 110, false, false, true);

            // 공격 Lord 생성
            LordMaker.MakeNewLord(
                faction,
                new LordJob_AssaultColony(faction, canKidnap: false, canTimeoutOrFlee: false),
                map,
                pawns
            );

            // 알림
            SendStandardLetter(parms, pawns);

            return true;
        }
    }
}