using RimWorld;
using RimWorld.QuestGen;
using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace LapinRace
{
    public class QuestPart_LapinSupplyVisitor : QuestPart_MakeLord
    {
        public ThingDef requestedThingDef;
        public int requestedAmount = 10;

        public IntVec3 waitCell;
        public int waitRadius = 6;
        public int leaveAfterTicks = 30000;

        public string outSignalItemsReceived;
        public string outSignalTimeout;

        private bool lordCreated = false;

        protected override Lord MakeLord()
        {
            if (lordCreated)
            {
                return null;
            }

            if (!waitCell.IsValid &&
                pawns != null &&
                pawns.Count > 0 &&
                pawns[0] != null)
            {
                waitCell = pawns[0].Position;
            }

            Lord lord = LordMaker.MakeNewLord(
                faction,
                new LordJob_LapinWaitForSupplies(
                    faction,
                    requestedThingDef,
                    requestedAmount,
                    waitCell,
                    waitRadius,
                    leaveAfterTicks,
                    outSignalItemsReceived,
                    outSignalTimeout
                ),
                base.Map,
                pawns
            );

            lordCreated = true;
            return lord;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_Defs.Look(ref requestedThingDef, "requestedThingDef");
            Scribe_Values.Look(ref requestedAmount, "requestedAmount", 10);
            Scribe_Values.Look(ref waitCell, "waitCell");
            Scribe_Values.Look(ref waitRadius, "waitRadius", 6);
            Scribe_Values.Look(ref leaveAfterTicks, "leaveAfterTicks", 30000);
            Scribe_Values.Look(ref outSignalItemsReceived, "outSignalItemsReceived");
            Scribe_Values.Look(ref outSignalTimeout, "outSignalTimeout");
            Scribe_Values.Look(ref lordCreated, "lordCreated", false);
        }
    }

    public class LordJob_LapinWaitForSupplies : LordJob
    {
        public Faction faction;
        public ThingDef requestedThingDef;
        public int requestedAmount;

        public IntVec3 waitCell;
        public int waitRadius;
        public int leaveAfterTicks;

        public string outSignalItemsReceived;
        public string outSignalTimeout;

        public LordJob_LapinWaitForSupplies()
        {
        }

        public LordJob_LapinWaitForSupplies(
            Faction faction,
            ThingDef requestedThingDef,
            int requestedAmount,
            IntVec3 waitCell,
            int waitRadius,
            int leaveAfterTicks,
            string outSignalItemsReceived,
            string outSignalTimeout)
        {
            this.faction = faction;
            this.requestedThingDef = requestedThingDef;
            this.requestedAmount = requestedAmount;
            this.waitCell = waitCell;
            this.waitRadius = waitRadius;
            this.leaveAfterTicks = leaveAfterTicks;
            this.outSignalItemsReceived = outSignalItemsReceived;
            this.outSignalTimeout = outSignalTimeout;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            LordToil_LapinWaitForSupplies toil = new LordToil_LapinWaitForSupplies(
                requestedThingDef,
                requestedAmount,
                waitCell,
                waitRadius,
                leaveAfterTicks,
                outSignalItemsReceived,
                outSignalTimeout
            );

            graph.AddToil(toil);
            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            Scribe_References.Look(ref faction, "faction");
            Scribe_Defs.Look(ref requestedThingDef, "requestedThingDef");
            Scribe_Values.Look(ref requestedAmount, "requestedAmount", 10);
            Scribe_Values.Look(ref waitCell, "waitCell");
            Scribe_Values.Look(ref waitRadius, "waitRadius", 6);
            Scribe_Values.Look(ref leaveAfterTicks, "leaveAfterTicks", 30000);
            Scribe_Values.Look(ref outSignalItemsReceived, "outSignalItemsReceived");
            Scribe_Values.Look(ref outSignalTimeout, "outSignalTimeout");
        }
    }

    public class LordToil_LapinWaitForSupplies : LordToil
    {
        public ThingDef requestedThingDef;
        public int requestedAmount;

        public IntVec3 waitCell;
        public int waitRadius;
        public int leaveAfterTicks;

        public string outSignalItemsReceived;
        public string outSignalTimeout;

        private int ticksPassed;
        private int nextWanderTick;
        private bool finished;

        private Dictionary<Pawn, float> lastHealth = new Dictionary<Pawn, float>();

        public LordToil_LapinWaitForSupplies()
        {
        }

        public LordToil_LapinWaitForSupplies(
            ThingDef requestedThingDef,
            int requestedAmount,
            IntVec3 waitCell,
            int waitRadius,
            int leaveAfterTicks,
            string outSignalItemsReceived,
            string outSignalTimeout)
        {
            this.requestedThingDef = requestedThingDef;
            this.requestedAmount = requestedAmount;
            this.waitCell = waitCell;
            this.waitRadius = waitRadius;
            this.leaveAfterTicks = leaveAfterTicks;
            this.outSignalItemsReceived = outSignalItemsReceived;
            this.outSignalTimeout = outSignalTimeout;

            ticksPassed = 0;
            nextWanderTick = Find.TickManager.TicksGame + 300;
            finished = false;
        }

        public override void UpdateAllDuties()
        {
            if (lord == null || lord.ownedPawns == null)
            {
                return;
            }

            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn == null || pawn.Dead || pawn.Destroyed)
                {
                    continue;
                }

                pawn.mindState.duty = new PawnDuty(
                    DutyDefOf.TravelOrWait,
                    waitCell
                );
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();

            if (finished ||
                lord == null ||
                lord.ownedPawns == null ||
                lord.ownedPawns.Count == 0)
            {
                return;
            }

            Pawn pawn = GetValidPawn();

            if (pawn == null)
            {
                Finish(false, null);
                return;
            }

            if (!waitCell.IsValid)
            {
                waitCell = pawn.Position;
            }

            if (AnyLapinWasDamaged())
            {
                BreakDealAndAttack();
                return;
            }

            ticksPassed++;

            if (TryTakeRequestedItems(pawn))
            {
                Finish(true, pawn);
                return;
            }

            if (ticksPassed >= leaveAfterTicks)
            {
                Finish(false, pawn);
                return;
            }

            DoSlowWander(pawn);
        }

        private Pawn GetValidPawn()
        {
            return lord?.ownedPawns?.FirstOrDefault(p =>
                p != null &&
                p.Spawned &&
                !p.Dead &&
                !p.Destroyed
            );
        }

        private bool AnyLapinWasDamaged()
        {
            if (lord == null || lord.ownedPawns == null)
            {
                return false;
            }

            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn == null || pawn.Destroyed || pawn.Dead)
                {
                    continue;
                }

                float currentHealth = pawn.health.summaryHealth.SummaryHealthPercent;

                if (!lastHealth.ContainsKey(pawn))
                {
                    lastHealth[pawn] = currentHealth;
                    continue;
                }

                if (currentHealth < lastHealth[pawn] - 0.001f)
                {
                    lastHealth[pawn] = currentHealth;
                    return true;
                }

                lastHealth[pawn] = currentHealth;
            }

            return false;
        }

        private void BreakDealAndAttack()
        {
            if (finished)
            {
                return;
            }

            finished = true;

            Faction faction = null;

            if (lord != null &&
                lord.ownedPawns != null &&
                lord.ownedPawns.Count > 0)
            {
                Pawn pawn = lord.ownedPawns.FirstOrDefault(p =>
                    p != null &&
                    p.Faction != null &&
                    p.Faction != Faction.OfPlayer
                );

                if (pawn != null)
                {
                    faction = pawn.Faction;
                }
            }

            if (faction != null &&
                faction != Faction.OfPlayer)
            {
                faction.TryAffectGoodwillWith(
                    Faction.OfPlayer,
                    -90,
                    canSendMessage: true,
                    canSendHostilityLetter: true,
                    reason: HistoryEventDefOf.AttackedMember
                );
            }

            Messages.Message(
                "라핀 대표자 일행이 공격을 받아 보급 거래가 결렬되었습니다!",
                MessageTypeDefOf.ThreatBig
            );

            StartAggressiveCombat();
        }

        private bool TryTakeRequestedItems(Pawn pawn)
        {
            if (pawn == null ||
                pawn.Map == null ||
                requestedThingDef == null ||
                requestedAmount <= 0)
            {
                return false;
            }

            Map map = pawn.Map;
            int remaining = requestedAmount;
            List<Thing> foundThings = new List<Thing>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(pawn.Position, 2.9f, true))
            {
                if (!cell.InBounds(map))
                {
                    continue;
                }

                List<Thing> things = cell.GetThingList(map);

                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];

                    if (thing != null &&
                        thing.Spawned &&
                        thing.def == requestedThingDef)
                    {
                        foundThings.Add(thing);
                        remaining -= thing.stackCount;

                        if (remaining <= 0)
                        {
                            break;
                        }
                    }
                }

                if (remaining <= 0)
                {
                    break;
                }
            }

            if (remaining > 0)
            {
                return false;
            }

            int needToConsume = requestedAmount;

            foreach (Thing thing in foundThings)
            {
                if (thing == null || thing.Destroyed)
                {
                    continue;
                }

                int take = System.Math.Min(needToConsume, thing.stackCount);
                needToConsume -= take;

                if (take >= thing.stackCount)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
                else
                {
                    thing.stackCount -= take;
                }

                if (needToConsume <= 0)
                {
                    break;
                }
            }

            return true;
        }

        private void DoSlowWander(Pawn pawn)
        {
            if (pawn == null || pawn.Map == null)
            {
                return;
            }

            int now = Find.TickManager.TicksGame;

            if (now < nextWanderTick)
            {
                return;
            }

            nextWanderTick = now + Rand.Range(1800, 3200);

            Map map = pawn.Map;
            IntVec3 destination;

            bool tooFar = pawn.Position.DistanceTo(waitCell) > waitRadius + 2;

            if (tooFar)
            {
                destination = waitCell;
            }
            else if (!CellFinder.TryFindRandomCellNear(
                waitCell,
                map,
                waitRadius,
                c =>
                    c.InBounds(map) &&
                    c.Standable(map) &&
                    !c.Fogged(map) &&
                    pawn.CanReach(c, PathEndMode.OnCell, Danger.Some),
                out destination))
            {
                destination = waitCell;
            }

            Job job = JobMaker.MakeJob(JobDefOf.Goto, destination);
            job.locomotionUrgency = LocomotionUrgency.Walk;
            job.expiryInterval = Rand.Range(900, 1600);
            job.checkOverrideOnExpire = true;

            pawn.jobs.StopAll();
            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
        }

        private void Finish(bool success, Pawn representative)
        {
            if (finished)
            {
                return;
            }

            finished = true;

            if (success)
            {
                LapinSupplyUtility.GiveSupplyReward(representative);

                if (!outSignalItemsReceived.NullOrEmpty())
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalItemsReceived));
                }

                Messages.Message(
                    "라핀 대표자가 보급품을 받고 은 200개를 남겼습니다.",
                    representative,
                    MessageTypeDefOf.PositiveEvent
                );
            }
            else
            {
                if (!outSignalTimeout.NullOrEmpty())
                {
                    Find.SignalManager.SendSignal(new Signal(outSignalTimeout));
                }

                Messages.Message(
                    "라핀 대표자가 충분히 기다렸지만 보급품을 받지 못해 떠납니다.",
                    representative,
                    MessageTypeDefOf.NeutralEvent
                );
            }

            StartLeavingMap();
        }

        private void StartLeavingMap()
        {
            if (lord == null || lord.ownedPawns == null)
            {
                return;
            }

            List<Pawn> pawns = lord.ownedPawns
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

            Map map = pawns[0].Map;
            Faction faction = pawns[0].Faction;
            Lord oldLord = lord;

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

        private void StartAggressiveCombat()
        {
            if (lord == null || lord.ownedPawns == null)
            {
                return;
            }

            List<Pawn> pawns = lord.ownedPawns
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

            Map map = pawns[0].Map;
            Faction faction = pawns[0].Faction;
            Lord oldLord = lord;

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
                new LordJob_LapinSupplyAggressive(waitCell),
                map,
                pawns
            );
        }
    }

    public class LordJob_LapinSupplyAggressive : LordJob
    {
        public IntVec3 attackPoint;

        public LordJob_LapinSupplyAggressive()
        {
        }

        public LordJob_LapinSupplyAggressive(IntVec3 attackPoint)
        {
            this.attackPoint = attackPoint;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph graph = new StateGraph();

            graph.AddToil(
                new LordToil_LapinSupplyAggressive(attackPoint)
            );

            return graph;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref attackPoint, "attackPoint");
        }
    }

    public class LordToil_LapinSupplyAggressive : LordToil
    {
        public IntVec3 attackPoint;

        public LordToil_LapinSupplyAggressive()
        {
        }

        public LordToil_LapinSupplyAggressive(IntVec3 attackPoint)
        {
            this.attackPoint = attackPoint;
        }

        public override void UpdateAllDuties()
        {
            if (lord == null || lord.ownedPawns == null)
            {
                return;
            }

            foreach (Pawn pawn in lord.ownedPawns)
            {
                if (pawn == null ||
                    pawn.Dead ||
                    pawn.Destroyed)
                {
                    continue;
                }

                pawn.mindState.duty = new PawnDuty(
                    DutyDefOf.AssaultColony,
                    attackPoint
                );
            }
        }
    }
}