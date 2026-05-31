using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_AutomatonEnergyGate
    {
        private static readonly FieldInfo PathFollowerPawnField =
            AccessTools.Field(typeof(Pawn_PathFollower), "pawn");

        private static readonly FieldInfo JobTrackerPawnField =
            AccessTools.Field(typeof(Pawn_JobTracker), "pawn");

        static Patch_AutomatonEnergyGate()
        {
            Harmony harmony = new Harmony("LapinRace.AutomatonEnergyGate");

            MethodInfo startPath = AccessTools.Method(
                typeof(Pawn_PathFollower),
                "StartPath",
                new[] { typeof(LocalTargetInfo), typeof(PathEndMode) }
            );

            if (startPath != null)
            {
                harmony.Patch(
                    startPath,
                    prefix: new HarmonyMethod(typeof(Patch_AutomatonEnergyGate), nameof(StartPathPrefix))
                );
            }

            MethodInfo startJob = AccessTools.Method(typeof(Pawn_JobTracker), "StartJob");

            if (startJob != null)
            {
                harmony.Patch(
                    startJob,
                    prefix: new HarmonyMethod(typeof(Patch_AutomatonEnergyGate), nameof(StartJobPrefix))
                );
            }

            Log.Message("[LapinRace] Automaton energy gate patch loaded.");
        }

        private static bool StartPathPrefix(Pawn_PathFollower __instance)
        {
            Pawn pawn = PathFollowerPawnField?.GetValue(__instance) as Pawn;

            if (!IsAutomatonTank(pawn))
            {
                return true;
            }

            if (HasAutomatonEnergy(pawn))
            {
                return true;
            }

            __instance.StopDead();

            Messages.Message(
                "오토마톤 전차의 전력이 고갈되어 이동할 수 없습니다.",
                pawn,
                MessageTypeDefOf.RejectInput,
                false
            );

            return false;
        }

        private static bool StartJobPrefix(Pawn_JobTracker __instance, Job newJob)
        {
            Pawn pawn = JobTrackerPawnField?.GetValue(__instance) as Pawn;

            if (!IsAutomatonTank(pawn))
            {
                return true;
            }

            if (newJob == null || newJob.def == null)
            {
                return true;
            }

            if (!IsEnergyRequiredJob(newJob))
            {
                return true;
            }

            if (HasAutomatonEnergy(pawn))
            {
                return true;
            }

            if (pawn.pather != null)
            {
                pawn.pather.StopDead();
            }

            Messages.Message(
                "오토마톤 전차의 전력이 고갈되어 명령을 수행할 수 없습니다.",
                pawn,
                MessageTypeDefOf.RejectInput,
                false
            );

            return false;
        }

        private static bool IsEnergyRequiredJob(Job job)
        {
            string defName = job.def.defName;

            if (defName == "Goto")
            {
                return true;
            }

            if (defName == "AttackMelee")
            {
                return true;
            }

            if (defName == "AttackStatic")
            {
                return true;
            }

            if (defName.Contains("Attack"))
            {
                return true;
            }

            return false;
        }

        private static bool HasAutomatonEnergy(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            CompAutomatonEnergy comp = pawn.TryGetComp<CompAutomatonEnergy>();

            if (comp == null)
            {
                return true;
            }

            return comp.HasEnergy;
        }

        private static bool IsAutomatonTank(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null && pawn.def.defName == "LP_AutomatonTankRace")
            {
                return true;
            }

            if (pawn.kindDef != null)
            {
                string kindName = pawn.kindDef.defName;

                if (kindName == "LP_AutomatonTank_Player" ||
                    kindName == "LP_AutomatonTank")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
