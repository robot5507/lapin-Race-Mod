using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace LapinRace
{
    [DefOf]
    public static class LapinJobDefOf
    {
        public static JobDef LP_DeliverSupplyToLapinRepresentative;

        static LapinJobDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LapinJobDefOf));
        }
    }

    [StaticConstructorOnStartup]
    public static class LapinSupplyInteractionPatch
    {
        static LapinSupplyInteractionPatch()
        {
            Harmony harmony = new Harmony("LapinRace.SupplyInteraction");

            var targetMethod = AccessTools.Method(
                typeof(FloatMenuMakerMap),
                "GetOptions"
            );

            if (targetMethod == null)
            {
                Log.Error("[LapinRace] FloatMenuMakerMap.GetOptions 메서드를 찾지 못했습니다.");
                return;
            }

            harmony.Patch(
                targetMethod,
                postfix: new HarmonyMethod(
                    typeof(LapinSupplyInteractionPatch),
                    nameof(AddLapinSupplyOption)
                )
            );

            Log.Message("[LapinRace] 라핀 보급 상호작용 패치 적용 완료: FloatMenuMakerMap.GetOptions");
        }

        public static void AddLapinSupplyOption(
            List<Pawn> selectedPawns,
            Vector3 clickPos,
            ref FloatMenuContext context,
            ref List<FloatMenuOption> __result
        )
        {
            if (__result == null ||
                selectedPawns == null ||
                selectedPawns.Count == 0)
            {
                return;
            }

            Pawn actor = selectedPawns[0];

            if (actor == null ||
                actor.Map == null ||
                actor.Faction != Faction.OfPlayer)
            {
                return;
            }

            Map map = actor.Map;
            IntVec3 clickedCell = IntVec3.FromVector3(clickPos);

            if (!clickedCell.InBounds(map))
            {
                return;
            }

            Pawn target = clickedCell
                .GetThingList(map)
                .OfType<Pawn>()
                .FirstOrDefault(p =>
                    p != null &&
                    !p.Dead &&
                    !p.Destroyed &&
                    p.kindDef != null &&
                    p.kindDef.defName == "LapinNobleOfficer"
                );

            if (target == null)
            {
                return;
            }

            ThingDef requestedSupply = LapinSupplyUtility.GetRequestedSupplyFromRepresentative(target);
            Thing supply = FindSupplyStack(actor, requestedSupply);

            if (supply == null)
            {
                __result.Add(new FloatMenuOption(
                    "라핀 대표자에게 보급품 전달하기: " +
                    requestedSupply.label + " x" + LapinSupplyUtility.RequiredAmount + " 필요",
                    null
                ));

                return;
            }

            __result.Add(new FloatMenuOption(
                "라핀 대표자에게 " +
                requestedSupply.label + " x" + LapinSupplyUtility.RequiredAmount + " 전달하기",
                delegate
                {
                    Job job = JobMaker.MakeJob(
                        LapinJobDefOf.LP_DeliverSupplyToLapinRepresentative,
                        target,
                        supply
                    );

                    job.count = LapinSupplyUtility.RequiredAmount;

                    actor.jobs.TryTakeOrderedJob(
                        job,
                        JobTag.Misc
                    );
                },
                MenuOptionPriority.High,
                null,
                target
            ));
        }

        private static Thing FindSupplyStack(Pawn pawn, ThingDef requestedThingDef)
        {
            if (pawn == null ||
                pawn.Map == null ||
                requestedThingDef == null)
            {
                return null;
            }

            foreach (Thing thing in pawn.Map.listerThings.ThingsOfDef(requestedThingDef))
            {
                if (thing == null ||
                    thing.Destroyed ||
                    !thing.Spawned ||
                    thing.stackCount < LapinSupplyUtility.RequiredAmount ||
                    thing.IsForbidden(pawn))
                {
                    continue;
                }

                if (pawn.CanReserveAndReach(
                    thing,
                    PathEndMode.ClosestTouch,
                    Danger.Some))
                {
                    return thing;
                }
            }

            return null;
        }
    }

    public class JobDriver_DeliverSupplyToLapinRepresentative : JobDriver
    {
        private const TargetIndex RepresentativeIndex = TargetIndex.A;
        private const TargetIndex SupplyIndex = TargetIndex.B;

        private Pawn Representative => job.GetTarget(RepresentativeIndex).Pawn;
        private Thing Supply => job.GetTarget(SupplyIndex).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            if (!pawn.Reserve(Supply, job, 1, job.count, null, errorOnFailed))
            {
                return false;
            }

            pawn.Reserve(Representative, job, 1, -1, null, false);
            return true;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(RepresentativeIndex);
            this.FailOn(() =>
                Representative == null ||
                Representative.Dead ||
                !Representative.Spawned
            );

            Toil gotoSupply = Toils_Goto.GotoThing(
                SupplyIndex,
                PathEndMode.ClosestTouch
            );

            gotoSupply.FailOnDestroyedOrNull(SupplyIndex);
            gotoSupply.FailOnDespawnedNullOrForbidden(SupplyIndex);

            yield return gotoSupply;

            Toil startCarry = Toils_Haul.StartCarryThing(
                SupplyIndex,
                false,
                true,
                false,
                true,
                false
            );

            startCarry.FailOnDestroyedOrNull(SupplyIndex);
            startCarry.FailOnDespawnedNullOrForbidden(SupplyIndex);

            yield return startCarry;

            yield return Toils_Goto.GotoThing(
                RepresentativeIndex,
                PathEndMode.Touch
            );

            Toil giveSupply = new Toil();
            giveSupply.initAction = delegate
            {
                Thing carried = pawn.carryTracker.CarriedThing;

                if (carried == null || carried.Destroyed)
                {
                    EndJobWith(JobCondition.Incompletable);
                    return;
                }

                carried.Destroy(DestroyMode.Vanish);

                LapinSupplyUtility.GiveSupplyReward(Representative);

                Find.SignalManager.SendSignal(
                    new Signal("LapinSuppliesReceived")
                );

                Messages.Message(
                    "라핀 대표자가 보급품을 받고 은 200개를 남겼습니다.",
                    Representative,
                    MessageTypeDefOf.PositiveEvent
                );

                StartLapinGroupLeaving();
            };

            giveSupply.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return giveSupply;
        }

        private void StartLapinGroupLeaving()
        {
            Pawn representative = Representative;

            if (representative == null ||
                representative.Map == null ||
                representative.Dead ||
                representative.Destroyed)
            {
                return;
            }

            Lord oldLord = representative.GetLord();

            if (oldLord == null || oldLord.ownedPawns == null)
            {
                return;
            }

            List<Pawn> pawns = oldLord.ownedPawns
                .Where(p =>
                    p != null &&
                    p.Spawned &&
                    !p.Dead &&
                    !p.Destroyed)
                .ToList();

            if (pawns.Count == 0)
            {
                return;
            }

            Map map = representative.Map;
            Faction faction = representative.Faction;

            foreach (Pawn pawn in pawns)
            {
                oldLord.Notify_PawnLost(
                    pawn,
                    PawnLostCondition.ForcedByPlayerAction,
                    null
                );
            }

            LordMaker.MakeNewLord(
                faction,
                new LordJob_ExitMapBest(LocomotionUrgency.Walk),
                map,
                pawns
            );
        }
    }
}