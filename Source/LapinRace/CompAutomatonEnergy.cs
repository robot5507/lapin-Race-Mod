using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class CompProperties_AutomatonEnergy : CompProperties
    {
        public float maxEnergy = 100f;
        public float initialEnergy = 50f;

        // 화학연료 1개당 충전되는 오토마톤 전력량
        public float chemfuelToEnergy = 10f;

        // tick당 기본 전력 소모량
        public float energyDrainPerTick = 0.0005f;

        public CompProperties_AutomatonEnergy()
        {
            compClass = typeof(CompAutomatonEnergy);
        }
    }

    public class CompAutomatonEnergy : ThingComp
    {
        private float energy = -1f;

        private CompProperties_AutomatonEnergy Props
        {
            get
            {
                return (CompProperties_AutomatonEnergy)props;
            }
        }

        private Pawn Pawn
        {
            get
            {
                return parent as Pawn;
            }
        }

        public float Energy
        {
            get
            {
                InitializeEnergyIfNeeded();
                return energy;
            }
        }

        public float MaxEnergy
        {
            get
            {
                return Props.maxEnergy;
            }
        }

        public bool HasEnergy
        {
            get
            {
                return Energy > 0.01f;
            }
        }

        public bool IsFull
        {
            get
            {
                return Energy >= MaxEnergy - 0.01f;
            }
        }

        public float ChemfuelToEnergy
        {
            get
            {
                return Props.chemfuelToEnergy;
            }
        }

        public int NeededChemfuelCount
        {
            get
            {
                InitializeEnergyIfNeeded();

                if (Props.chemfuelToEnergy <= 0f)
                {
                    return 0;
                }

                float missing = MaxEnergy - Energy;

                if (missing <= 0f)
                {
                    return 0;
                }

                return Mathf.CeilToInt(missing / Props.chemfuelToEnergy);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref energy, "energy", -1f);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeEnergyIfNeeded();

            Pawn pawn = Pawn;
            if (pawn != null)
            {
                SyncVanillaMechEnergyNeed(pawn);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                return;
            }

            InitializeEnergyIfNeeded();

            float drain = 0f;

            // 대기 상태에서는 전력을 소모하지 않는다.
            // 소집 중일 때만 기본 소모
            if (pawn.Drafted)
            {
                drain += Props.energyDrainPerTick * 2f;
            }

            // 이동 중이면 추가 소모
            if (pawn.pather != null && pawn.pather.Moving)
            {
                drain += Props.energyDrainPerTick * 5f;
            }

            // 전투 Job 중이면 추가 소모
            if (pawn.CurJob != null && pawn.CurJob.def != null)
            {
                string jobName = pawn.CurJob.def.defName;

                if (jobName == "AttackMelee" ||
                    jobName == "AttackStatic" ||
                    jobName.Contains("Attack"))
                {
                    drain += Props.energyDrainPerTick * 5f;
                }
            }

            if (drain > 0f)
            {
                energy -= drain;

                if (energy <= 0f)
                {
                    energy = 0f;
                    StopEnergyUsingJobs(pawn);
                }
            }

            SyncVanillaMechEnergyNeed(pawn);
        }

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (Gizmo gizmo in base.CompGetGizmosExtra())
            {
                yield return gizmo;
            }

            Pawn pawn = Pawn;
            if (pawn == null || pawn.Dead)
            {
                yield break;
            }

            yield return new Gizmo_AutomatonEnergyStatus(this);
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            Pawn automaton = Pawn;

            if (automaton == null || automaton.Dead || automaton.Map == null)
            {
                yield break;
            }

            if (selPawn == null || selPawn.Map != automaton.Map)
            {
                yield break;
            }

            if (selPawn.Faction == null || !selPawn.Faction.IsPlayer)
            {
                yield break;
            }

            if (selPawn.Dead || selPawn.Downed)
            {
                yield break;
            }

            if (IsFull)
            {
                yield return new FloatMenuOption("화학연료 주입: 전력이 이미 가득 참", null);
                yield break;
            }

            if (!selPawn.CanReserveAndReach(automaton, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("화학연료 주입: 전차에 접근할 수 없음", null);
                yield break;
            }

            Thing fuel = FindReachableChemfuelFor(selPawn);
            if (fuel == null)
            {
                yield return new FloatMenuOption("화학연료 주입: 사용할 수 있는 화학연료 없음", null);
                yield break;
            }

            yield return new FloatMenuOption("화학연료 주입", delegate
            {
                JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_RefuelAutomaton");
                if (jobDef == null)
                {
                    Log.Error("[LapinRace] LP_RefuelAutomaton JobDef를 찾을 수 없습니다.");
                    return;
                }

                Job job = JobMaker.MakeJob(jobDef, automaton, fuel);
                job.count = 1;
                selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            });

            int neededFuel = NeededChemfuelCount;

            if (neededFuel > 1)
            {
                int availableInStack = fuel.stackCount;
                int countToUse = neededFuel;

                if (countToUse > availableInStack)
                {
                    countToUse = availableInStack;
                }

                yield return new FloatMenuOption("화학연료 가득 주입 (" + countToUse + "개)", delegate
                {
                    JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_RefuelAutomaton");
                    if (jobDef == null)
                    {
                        Log.Error("[LapinRace] LP_RefuelAutomaton JobDef를 찾을 수 없습니다.");
                        return;
                    }

                    Job job = JobMaker.MakeJob(jobDef, automaton, fuel);
                    job.count = countToUse;
                    selPawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
                });
            }
        }

        private void InitializeEnergyIfNeeded()
        {
            if (energy < 0f)
            {
                energy = Mathf.Clamp(Props.initialEnergy, 0f, Props.maxEnergy);
            }
        }

        public float AddEnergy(float amount)
        {
            InitializeEnergyIfNeeded();

            if (amount <= 0f)
            {
                return 0f;
            }

            float before = energy;

            energy += amount;

            if (energy > Props.maxEnergy)
            {
                energy = Props.maxEnergy;
            }

            Pawn pawn = Pawn;
            if (pawn != null)
            {
                SyncVanillaMechEnergyNeed(pawn);
            }

            return energy - before;
        }

        private void StopEnergyUsingJobs(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            if (pawn.jobs == null || pawn.CurJob == null || pawn.CurJob.def == null)
            {
                return;
            }

            string jobName = pawn.CurJob.def.defName;

            if (jobName == "Goto" ||
                jobName == "AttackMelee" ||
                jobName == "AttackStatic" ||
                jobName.Contains("Attack"))
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        private void SyncVanillaMechEnergyNeed(Pawn pawn)
        {
            if (pawn == null || pawn.needs == null)
            {
                return;
            }

            float percent = 0f;

            if (MaxEnergy > 0f)
            {
                percent = Mathf.Clamp01(Energy / MaxEnergy);
            }

            List<Need> needs = pawn.needs.AllNeeds;
            if (needs == null)
            {
                return;
            }

            for (int i = 0; i < needs.Count; i++)
            {
                Need need = needs[i];

                if (need == null || need.def == null)
                {
                    continue;
                }

                string defName = need.def.defName;
                string typeName = need.GetType().Name;

                bool isMechEnergyNeed =
                    defName.Contains("MechEnergy") ||
                    defName.Contains("MechanoidEnergy") ||
                    typeName.Contains("MechEnergy") ||
                    typeName.Contains("MechanoidEnergy");

                if (!isMechEnergyNeed)
                {
                    continue;
                }

                need.CurLevel = need.MaxLevel * percent;
            }
        }

        private Thing FindReachableChemfuelFor(Pawn worker)
        {
            if (worker == null || worker.Map == null)
            {
                return null;
            }

            ThingDef chemfuelDef = ThingDefOf.Chemfuel;

            return GenClosest.ClosestThingReachable(
                worker.Position,
                worker.Map,
                ThingRequest.ForDef(chemfuelDef),
                PathEndMode.ClosestTouch,
                TraverseParms.For(worker, Danger.Deadly, TraverseMode.ByPawn),
                9999f,
                thing =>
                {
                    if (thing == null || thing.Destroyed)
                    {
                        return false;
                    }

                    if (thing.IsForbidden(worker))
                    {
                        return false;
                    }

                    if (thing.stackCount <= 0)
                    {
                        return false;
                    }

                    if (!worker.CanReserve(thing))
                    {
                        return false;
                    }

                    return true;
                }
            );
        }
    }

    public class Gizmo_AutomatonEnergyStatus : Gizmo
    {
        private readonly CompAutomatonEnergy comp;

        public Gizmo_AutomatonEnergyStatus(CompAutomatonEnergy comp)
        {
            this.comp = comp;
        }

        public override float GetWidth(float maxWidth)
        {
            return 140f;
        }

        public override GizmoResult GizmoOnGUI(Vector2 topLeft, float maxWidth, GizmoRenderParms parms)
        {
            Rect rect = new Rect(topLeft.x, topLeft.y, GetWidth(maxWidth), 75f);
            Widgets.DrawWindowBackground(rect);

            Rect labelRect = new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 24f);
            Widgets.Label(labelRect, "오토마톤 전력");

            float fillPercent = 0f;
            if (comp.MaxEnergy > 0f)
            {
                fillPercent = Mathf.Clamp01(comp.Energy / comp.MaxEnergy);
            }

            Rect barRect = new Rect(rect.x + 8f, rect.y + 38f, rect.width - 16f, 20f);
            Widgets.FillableBar(barRect, fillPercent);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(
                barRect,
                Mathf.RoundToInt(comp.Energy) + " / " + Mathf.RoundToInt(comp.MaxEnergy)
            );
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}