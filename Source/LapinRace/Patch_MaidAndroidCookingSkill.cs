using HarmonyLib;
using RimWorld;
using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class Patch_MaidAndroidCookingSkill
    {
        static Patch_MaidAndroidCookingSkill()
        {
            Harmony harmony = new Harmony("LapinRace.MaidAndroidCookingSkill");

            var recipeMethod = AccessTools.Method(
                typeof(RecipeDef),
                nameof(RecipeDef.PawnSatisfiesSkillRequirements)
            );

            if (recipeMethod != null)
            {
                harmony.Patch(
                    recipeMethod,
                    prefix: new HarmonyMethod(typeof(Patch_MaidAndroidCookingSkill), nameof(RecipeSkillPrefix))
                );
            }

            var skillReqMethod = AccessTools.Method(
                typeof(SkillRequirement),
                "PawnSatisfies",
                new[] { typeof(Pawn) }
            );

            if (skillReqMethod != null)
            {
                harmony.Patch(
                    skillReqMethod,
                    prefix: new HarmonyMethod(typeof(Patch_MaidAndroidCookingSkill), nameof(SkillRequirementPrefix))
                );
            }

            Log.Message("[LapinRace] Maid android cooking skill patch loaded.");
        }

        private static bool RecipeSkillPrefix(RecipeDef __instance, Pawn pawn, ref bool __result)
        {
            if (!IsMaidAndroid(pawn))
            {
                return true;
            }

            __result = MaidCanSatisfyRecipe(__instance, pawn);
            return false;
        }

        private static bool SkillRequirementPrefix(SkillRequirement __instance, Pawn pawn, ref bool __result)
        {
            if (!IsMaidAndroid(pawn))
            {
                return true;
            }

            if (__instance == null || __instance.skill == null)
            {
                __result = true;
                return false;
            }

            // 메이드는 조리 요구 조건만 가상 조리 레벨로 통과.
            if (__instance.skill == SkillDefOf.Cooking)
            {
                __result = GetVirtualCookingLevel(pawn) >= __instance.minLevel;
                return false;
            }

            // 다른 스킬 요구는 통과시키지 않음.
            __result = false;
            return false;
        }

        private static bool MaidCanSatisfyRecipe(RecipeDef recipe, Pawn pawn)
        {
            if (recipe == null)
            {
                return false;
            }

            if (recipe.skillRequirements == null || recipe.skillRequirements.Count == 0)
            {
                return true;
            }

            int cookingLevel = GetVirtualCookingLevel(pawn);

            for (int i = 0; i < recipe.skillRequirements.Count; i++)
            {
                SkillRequirement req = recipe.skillRequirements[i];

                if (req == null || req.skill == null)
                {
                    continue;
                }

                if (req.skill == SkillDefOf.Cooking)
                {
                    if (cookingLevel < req.minLevel)
                    {
                        return false;
                    }

                    continue;
                }

                return false;
            }

            return true;
        }

        private static int GetVirtualCookingLevel(Pawn pawn)
        {
            if (pawn == null)
            {
                return 0;
            }

            CompMaidAndroidVirtualCooking comp = pawn.TryGetComp<CompMaidAndroidVirtualCooking>();
            if (comp == null)
            {
                return 0;
            }

            return comp.CookingLevel;
        }

        private static bool IsMaidAndroid(Pawn pawn)
        {
            if (pawn == null)
            {
                return false;
            }

            if (pawn.def != null && pawn.def.defName == "LP_MaidAndroidRace")
            {
                return true;
            }

            if (pawn.kindDef != null && pawn.kindDef.defName == "LP_MaidAndroid")
            {
                return true;
            }

            return false;
        }
    }
}
