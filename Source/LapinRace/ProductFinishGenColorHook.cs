using HarmonyLib;
using LapinRace;
using System;
using UnityEngine;
using Verse;

namespace LapinRace
{
    // Token: 0x02000044 RID: 68
    [HarmonyPatch(typeof(GenRecipe))]
    [HarmonyPatch("PostProcessProduct")]
    public static class ProductFinishGenColorHook
    {
        // Token: 0x06000117 RID: 279 RVA: 0x0000A004 File Offset: 0x00008204
        [HarmonyPrefix]
        private static void Prefix(ref Thing product)
        {
            ThingWithComps twc = product as ThingWithComps;
            bool flag = twc != null;
            if (flag)
            {
                CustomThingDef def = twc.def as CustomThingDef;
                bool flag2 = def != null && !def.followStuffColor;
                if (flag2)
                {
                    CompColorableUtility.SetColor(twc, Color.white, true);
                }
            }
        }
    }
}