using System.Reflection;
using HarmonyLib;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class LP_HarmonyInit
    {
        static LP_HarmonyInit()
        {
            var harmony = new Harmony("lapinrace.earpatch");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            TryPatchAlienEarAppendDrawRequests(harmony);

            Log.Message("[LapinRace] Harmony init done");
        }

        private static void TryPatchAlienEarAppendDrawRequests(Harmony harmony)
        {
            var workerType =
                AccessTools.TypeByName("AlienRace.AlienPawnRenderNodeWorker_BodyAddon") ??
                AccessTools.TypeByName("AlienPawnRenderNodeWorker_BodyAddon");

            if (workerType == null)
            {
                Log.Warning("[LapinRace] Could not find AlienPawnRenderNodeWorker_BodyAddon");
                return;
            }

            var baseType = workerType.BaseType;
            if (baseType == null)
            {
                Log.Warning("[LapinRace] Worker base type is null");
                return;
            }

            MethodInfo appendMethod = AccessTools.Method(
                baseType,
                "AppendDrawRequests",
                new[]
                {
                    typeof(PawnRenderNode),
                    typeof(PawnDrawParms),
                    typeof(System.Collections.Generic.List<PawnGraphicDrawRequest>)
                });

            if (appendMethod == null)
            {
                Log.Warning("[LapinRace] Could not find AppendDrawRequests on worker base type");
                return;
            }

            var prefix = AccessTools.Method(
                typeof(LapinEarAppendDrawRequestsPatch),
                nameof(LapinEarAppendDrawRequestsPatch.Prefix));

            if (prefix == null)
            {
                Log.Warning("[LapinRace] Could not find LapinEarAppendDrawRequestsPatch.Prefix");
                return;
            }

            harmony.Patch(appendMethod, prefix: new HarmonyMethod(prefix));
            Log.Message("[LapinRace] Patched AppendDrawRequests on worker base");
        }
    }
}