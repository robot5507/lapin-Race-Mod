using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class CompProperties_PrototypeAndroidFuel : CompProperties
    {
        public float maxEnergy = 100f;
        public float initialEnergy = 50f;
        public float chemfuelToEnergy = 10f;

        // 작업 중 tick당 소모량
        public float energyDrainPerTick = 0.00035f;

        // 이 비율 아래로 내려가면 자동 보충 시도
        public float autoRefuelThreshold = 0.30f;

        public string fuelDefName = "Chemfuel";

        public CompProperties_PrototypeAndroidFuel()
        {
            compClass = typeof(CompPrototypeAndroidFuel);
        }
    }

    public class CompPrototypeAndroidFuel : ThingComp
    {
        private float energy = -1f;
        private bool autoRefuel = true;

        private CompProperties_PrototypeAndroidFuel Props
        {
            get
            {
                return (CompProperties_PrototypeAndroidFuel)props;
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

        public float ChemfuelToEnergy
        {
            get
            {
                return Props.chemfuelToEnergy;
            }
        }

        public bool AutoRefuel
        {
            get
            {
                return autoRefuel;
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

        public bool NeedsAutoRefuel
        {
            get
            {
                if (!autoRefuel)
                {
                    return false;
                }

                if (IsFull)
                {
                    return false;
                }

                return Energy <= MaxEnergy * Props.autoRefuelThreshold;
            }
        }

        public int NeededFuelCount
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

        public ThingDef FuelDef
        {
            get
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail(Props.fuelDefName);
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref energy, "energy", -1f);
            Scribe_Values.Look(ref autoRefuel, "autoRefuel", true);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeEnergyIfNeeded();
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

            // 작업 중일 때만 전력 소모
            if (pawn.CurJob != null && pawn.CurJob.def != null)
            {
                string jobName = pawn.CurJob.def.defName;

                if (jobName.Contains("Haul") ||
                    jobName.Contains("Carry") ||
                    jobName == "Goto")
                {
                    drain += Props.energyDrainPerTick;
                }
            }

            if (pawn.pather != null && pawn.pather.Moving)
            {
                drain += Props.energyDrainPerTick;
            }

            if (drain > 0f)
            {
                energy -= drain;

                if (energy <= 0f)
                {
                    energy = 0f;
                    StopCurrentWork(pawn);
                }
            }
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

            yield return new Gizmo_PrototypeAndroidEnergyStatus(this);

            yield return new Command_Toggle
            {
                defaultLabel = "자동 연료 보충",
                defaultDesc = "켜두면 전력이 낮을 때 시제형 안드로이드가 스스로 화학연료를 찾아가 보충합니다.",
                icon = TexCommand.DesirePower,
                isActive = () => autoRefuel,
                toggleAction = delegate
                {
                    autoRefuel = !autoRefuel;
                }
            };
        
        }

        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn))
            {
                yield return option;
            }

            Pawn android = Pawn;

            if (android == null || android.Dead || android.Map == null)
            {
                yield break;
            }

            if (selPawn == null || selPawn.Map != android.Map)
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

            if (!selPawn.CanReserveAndReach(android, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("화학연료 주입: 안드로이드에 접근할 수 없음", null);
                yield break;
            }

            Thing fuel = FindReachableFuelFor(selPawn);
            if (fuel == null)
            {
                yield return new FloatMenuOption("화학연료 주입: 사용할 수 있는 화학연료 없음", null);
                yield break;
            }

            yield return new FloatMenuOption("화학연료 주입", delegate
            {
                StartRefuelJob(selPawn, android, fuel, 1);
            });

            int neededFuel = NeededFuelCount;

            if (neededFuel > 1)
            {
                int countToUse = neededFuel;

                if (countToUse > fuel.stackCount)
                {
                    countToUse = fuel.stackCount;
                }

                yield return new FloatMenuOption("화학연료 가득 주입 (" + countToUse + "개)", delegate
                {
                    StartRefuelJob(selPawn, android, fuel, countToUse);
                });
            }
        }

        private void StartRefuelJob(Pawn worker, Pawn target, Thing fuel, int count)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_RefuelAutomaton");
            if (jobDef == null)
            {
                Log.Error("[LapinRace] LP_RefuelAutomaton JobDef를 찾을 수 없습니다.");
                return;
            }

            Job job = JobMaker.MakeJob(jobDef, target, fuel);
            job.count = Mathf.Max(1, count);
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
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

            return energy - before;
        }

        public void InitializeEnergyIfNeeded()
        {
            if (energy < 0f)
            {
                energy = Mathf.Clamp(Props.initialEnergy, 0f, Props.maxEnergy);
            }
        }

        private void StopCurrentWork(Pawn pawn)
        {
            if (pawn == null)
            {
                return;
            }

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            if (pawn.jobs != null && pawn.CurJob != null)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }

        public Thing FindReachableFuelFor(Pawn worker)
        {
            if (worker == null || worker.Map == null)
            {
                return null;
            }

            ThingDef fuelDef = FuelDef;
            if (fuelDef == null)
            {
                Log.Error("[LapinRace] 연료 ThingDef를 찾을 수 없습니다: " + Props.fuelDefName);
                return null;
            }

            return GenClosest.ClosestThingReachable(
                worker.Position,
                worker.Map,
                ThingRequest.ForDef(fuelDef),
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

    public class Gizmo_PrototypeAndroidEnergyStatus : Gizmo
    {
        private readonly CompPrototypeAndroidFuel comp;

        public Gizmo_PrototypeAndroidEnergyStatus(CompPrototypeAndroidFuel comp)
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
            Widgets.Label(labelRect, "안드로이드 전력");

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