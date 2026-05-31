using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace LapinRace
{
    public class CompProperties_AutomatonAmmo : CompProperties
    {
        public string ammoDefName = "LP_Cannonball";
        public int maxAmmo = 25;

        public CompProperties_AutomatonAmmo()
        {
            compClass = typeof(CompAutomatonAmmo);
        }
    }

    public class CompAutomatonAmmo : ThingComp
    {
        private int ammo = -1;

        private CompProperties_AutomatonAmmo Props
        {
            get
            {
                return (CompProperties_AutomatonAmmo)props;
            }
        }

        private Pawn Pawn
        {
            get
            {
                return parent as Pawn;
            }
        }

        public int Ammo
        {
            get
            {
                InitializeAmmoIfNeeded();
                return ammo;
            }
        }

        public int MaxAmmo
        {
            get
            {
                return Props.maxAmmo;
            }
        }

        public int NeededAmmoCount
        {
            get
            {
                return Mathf.Max(0, MaxAmmo - Ammo);
            }
        }

        public ThingDef AmmoDef
        {
            get
            {
                return DefDatabase<ThingDef>.GetNamedSilentFail(Props.ammoDefName);
            }
        }

        public bool HasAmmo
        {
            get
            {
                return Ammo > 0;
            }
        }

        public bool IsFull
        {
            get
            {
                return Ammo >= MaxAmmo;
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref ammo, "ammo", -1);
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            InitializeAmmoIfNeeded();
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

            yield return new Gizmo_AutomatonAmmoStatus(this);
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
                yield return new FloatMenuOption("포탄 적재: 이미 가득 참", null);
                yield break;
            }

            if (!selPawn.CanReserveAndReach(automaton, PathEndMode.Touch, Danger.Deadly))
            {
                yield return new FloatMenuOption("포탄 적재: 전차에 접근할 수 없음", null);
                yield break;
            }

            Thing ammoThing = FindReachableAmmoFor(selPawn);
            if (ammoThing == null)
            {
                yield return new FloatMenuOption("포탄 적재: 사용할 수 있는 라핀 대포 포탄 없음", null);
                yield break;
            }

            yield return new FloatMenuOption("포탄 적재", delegate
            {
                StartLoadAmmoJob(selPawn, automaton, ammoThing, 1);
            });

            int neededAmmo = NeededAmmoCount;
            if (neededAmmo > 1)
            {
                int countToUse = neededAmmo;
                if (countToUse > ammoThing.stackCount)
                {
                    countToUse = ammoThing.stackCount;
                }

                yield return new FloatMenuOption("포탄 가득 적재 (" + countToUse + "발)", delegate
                {
                    StartLoadAmmoJob(selPawn, automaton, ammoThing, countToUse);
                });
            }
        }

        private void StartLoadAmmoJob(Pawn worker, Pawn automaton, Thing ammoThing, int count)
        {
            JobDef jobDef = DefDatabase<JobDef>.GetNamedSilentFail("LP_LoadAutomatonAmmo");
            if (jobDef == null)
            {
                Log.Error("[LapinRace] LP_LoadAutomatonAmmo JobDef를 찾을 수 없습니다.");
                return;
            }

            Job job = JobMaker.MakeJob(jobDef, automaton, ammoThing);
            job.count = Mathf.Max(1, count);
            worker.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }

        private void InitializeAmmoIfNeeded()
        {
            if (ammo < 0)
            {
                ammo = 0;
            }

            if (ammo > MaxAmmo)
            {
                ammo = MaxAmmo;
            }
        }

        public int AddAmmo(int count)
        {
            InitializeAmmoIfNeeded();

            if (count <= 0)
            {
                return 0;
            }

            int before = ammo;
            ammo += count;

            if (ammo > MaxAmmo)
            {
                ammo = MaxAmmo;
            }

            return ammo - before;
        }

        public bool TryConsumeAmmo(int count)
        {
            InitializeAmmoIfNeeded();

            if (count <= 0)
            {
                return true;
            }

            if (ammo < count)
            {
                return false;
            }

            ammo -= count;
            if (ammo < 0)
            {
                ammo = 0;
            }

            return true;
        }

        private Thing FindReachableAmmoFor(Pawn worker)
        {
            if (worker == null || worker.Map == null)
            {
                return null;
            }

            ThingDef ammoDef = AmmoDef;
            if (ammoDef == null)
            {
                Log.Error("[LapinRace] 포탄 ThingDef를 찾을 수 없습니다: " + Props.ammoDefName);
                return null;
            }

            return GenClosest.ClosestThingReachable(
                worker.Position,
                worker.Map,
                ThingRequest.ForDef(ammoDef),
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

    public class Gizmo_AutomatonAmmoStatus : Gizmo
    {
        private readonly CompAutomatonAmmo comp;

        public Gizmo_AutomatonAmmoStatus(CompAutomatonAmmo comp)
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
            Widgets.Label(labelRect, "전차포 포탄");

            float fillPercent = 0f;
            if (comp.MaxAmmo > 0)
            {
                fillPercent = Mathf.Clamp01((float)comp.Ammo / comp.MaxAmmo);
            }

            Rect barRect = new Rect(rect.x + 8f, rect.y + 38f, rect.width - 16f, 20f);
            Widgets.FillableBar(barRect, fillPercent);

            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(barRect, comp.Ammo + " / " + comp.MaxAmmo);
            Text.Anchor = TextAnchor.UpperLeft;

            return new GizmoResult(GizmoState.Clear);
        }
    }
}
