using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace LapinRace
{
    public class IncidentWorker_LapinSupplyRequest : IncidentWorker
    {
        protected override bool CanFireNowSub(IncidentParms parms)
        {
            return parms.target is Map;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = parms.target as Map;

            if (map == null)
            {
                return false;
            }

            PawnKindDef officerKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinNobleOfficer");
            PawnKindDef soldierKind = DefDatabase<PawnKindDef>.GetNamedSilentFail("LapinSoldier");

            if (officerKind == null)
            {
                Log.Error("[LapinRace] PawnKindDef 'LapinNobleOfficer'를 찾을 수 없습니다.");
                return false;
            }

            if (soldierKind == null)
            {
                Log.Error("[LapinRace] PawnKindDef 'LapinSoldier'를 찾을 수 없습니다.");
                return false;
            }

            FactionDef factionDef = DefDatabase<FactionDef>.GetNamedSilentFail("LapinEmpire");
            Faction faction = factionDef != null ? Find.FactionManager.FirstFactionOfDef(factionDef) : null;

            if (faction == null)
            {
                faction = Faction.OfPlayer;
            }

            IntVec3 spawnCell;

            if (!TryFindSafeInnerSpawnCell(map, out spawnCell))
            {
                Log.Warning("[LapinRace] 라핀 보급 요청 대표자 스폰 위치를 찾지 못했습니다.");
                return false;
            }

            Pawn officer = GeneratePawn(officerKind, faction);
            Pawn guardA = GeneratePawn(soldierKind, faction);
            Pawn guardB = GeneratePawn(soldierKind, faction);

            GenSpawn.Spawn(officer, spawnCell, map);
            GenSpawn.Spawn(guardA, CellFinder.RandomClosewalkCellNear(spawnCell, map, 3), map);
            GenSpawn.Spawn(guardB, CellFinder.RandomClosewalkCellNear(spawnCell, map, 3), map);

            List<Pawn> group = new List<Pawn>
            {
                officer,
                guardA,
                guardB
            };

            ThingDef requestedSupply = LapinSupplyUtility.RandomRequestedSupply();

            LordMaker.MakeNewLord(
                faction,
                new LordJob_LapinWaitForSupplies(
                    faction,
                    requestedSupply,
                    LapinSupplyUtility.RequiredAmount,
                    spawnCell,
                    6,
                    30000,
                    "LapinSuppliesReceived",
                    "LapinSuppliesTimeout"
                ),
                map,
                group
            );

            Find.LetterStack.ReceiveLetter(
              "라핀 보급 요청",
              "라핀 귀족 장교가 호위병들과 함께 정착지에 도착했습니다.\n\n" +
              "요구 물품:\n" +
              requestedSupply.LabelCap +
              " x" +
              LapinSupplyUtility.RequiredAmount +
              "\n\n" +
              "보급품을 전달하면 은화와 우호도를 얻을 수 있습니다.",
              LetterDefOf.PositiveEvent,
              new LookTargets(officer),
              faction
          );

            return true;
        }

        private static Pawn GeneratePawn(PawnKindDef kind, Faction faction)
        {
            PawnGenerationRequest request = new PawnGenerationRequest(
                kind,
                faction,
                PawnGenerationContext.NonPlayer,
                -1,
                forceGenerateNewPawn: true,
                allowDead: false,
                allowDowned: false,
                canGeneratePawnRelations: false,
                mustBeCapableOfViolence: false
            );

            return PawnGenerator.GeneratePawn(request);
        }

        private static bool TryFindSafeInnerSpawnCell(Map map, out IntVec3 result)
        {
            for (int i = 0; i < 80; i++)
            {
                IntVec3 edgeCell;

                if (!CellFinder.TryFindRandomEdgeCellWith(
                    c => c.Standable(map) && !c.Fogged(map),
                    map,
                    CellFinder.EdgeRoadChance_Neutral,
                    out edgeCell))
                {
                    continue;
                }

                IntVec3 center = map.Center;
                IntVec3 direction = center - edgeCell;
                IntVec3 candidate = edgeCell;

                int steps = Rand.RangeInclusive(12, 22);

                for (int s = 0; s < steps; s++)
                {
                    if (Mathf.Abs(direction.x) > Mathf.Abs(direction.z))
                    {
                        candidate.x += direction.x > 0 ? 1 : -1;
                    }
                    else
                    {
                        candidate.z += direction.z > 0 ? 1 : -1;
                    }
                }

                if (candidate.InBounds(map) &&
                    candidate.Standable(map) &&
                    !candidate.Fogged(map) &&
                    map.reachability.CanReachColony(candidate))
                {
                    result = candidate;
                    return true;
                }
            }

            result = IntVec3.Invalid;
            return false;
        }
    }
}