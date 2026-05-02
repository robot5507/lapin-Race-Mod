using RimWorld;
using System.Linq;
using Verse;

namespace LapinBaguette
{
    [StaticConstructorOnStartup]
    public static class HideBaguetteSmithyRecipePatch
    {
        static HideBaguetteSmithyRecipePatch()
        {
            try
            {
                foreach (RecipeDef recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
                {
                    if (recipe.products == null)
                        continue;

                    bool makesBaguette = recipe.products.Any(p =>
                        p.thingDef != null &&
                        p.thingDef.defName == "LP_Lapin_Bagette");

                    if (!makesBaguette || recipe.recipeUsers == null)
                        continue;

                    recipe.recipeUsers.RemoveAll(td =>
                        td != null &&
                        (td.defName == "ElectricSmithy" || td.defName == "FueledSmithy"));

                    Log.Message($"[Lapin Baguette] 단조대용 바게트 레시피 제거: {recipe.defName}");
                }
            }
            catch (System.Exception ex)
            {
                Log.Error($"[Lapin Baguette] 단조대 레시피 제거 실패: {ex}");
            }
        }
    }
}