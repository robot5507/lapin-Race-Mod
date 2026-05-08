using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace LapinRace
{
    public static class LapinEarAppendDrawRequestsPatch
    {
        public static bool Prefix(
            object __instance,
            PawnRenderNode node,
            PawnDrawParms parms,
            List<PawnGraphicDrawRequest> requests)
        {
            try
            {
                if (__instance == null || node == null || requests == null)
                    return true;

                string workerType = __instance.GetType().FullName ?? "";
                if (!workerType.Contains("AlienPawnRenderNodeWorker_BodyAddon"))
                    return true;

                Pawn pawn = node.tree?.pawn;
                if (pawn == null) return true;
                if (pawn.def?.defName != "Lapin") return true;

                bool wearingNormalEarHat =
                    pawn.apparel?.WornApparel.Any(a =>
                        a.def.defName == "LP_Hat_Grenadier" ||
                        a.def.defName == "LP_Hat_NobleOfficer" ||
                        a.def.defName == "LP_Hat_Artillery" ||
                        a.def.defName == "LP_Hat_Vlotigeur"
                    ) ?? false;

                bool wearingSapperHat =
                    pawn.apparel?.WornApparel.Any(a =>
                        a.def.defName == "LP_Hat_Sapper"
                    ) ?? false;

                bool wearingHat = wearingNormalEarHat || wearingSapperHat;

                // 침대에 누워있을 때는 East/West 반대쪽 귀 숨김을 적용하지 않음
                bool inBed =
                    pawn.CurrentBed() != null ||
                    pawn.GetPosture() == PawnPosture.LayingInBed;

                object props =
                    AccessTools.Property(node.GetType(), "Props")?.GetValue(node, null) ??
                    AccessTools.Field(node.GetType(), "props")?.GetValue(node);

                if (props == null) return true;

                object addon =
                    AccessTools.Field(props.GetType(), "addon")?.GetValue(props);

                if (addon == null) return true;

                string path =
                    AccessTools.Field(addon.GetType(), "path")?.GetValue(addon) as string;

                if (string.IsNullOrEmpty(path)) return true;

                bool isLeft = path.Contains("EarLeft");
                bool isRight = path.Contains("EarRight");
                bool isBackCut = path.Contains("BackCut");

                if (!isLeft && !isRight) return true;

                Rot4 rot = parms.facing;

                // BackCut 귀는 모자 없으면 절대 금지
                if (isBackCut && !wearingHat)
                {
                    return false;
                }

                // 모자 안 썼으면 기본 귀 그대로
                if (!wearingHat)
                {
                    return true;
                }

                // 공병 모자: North에서는 모든 귀 숨김
                if (wearingSapperHat && rot == Rot4.North)
                {
                    return false;
                }

                // 공병 모자: BackCut 귀는 아예 사용 안 함
                if (wearingSapperHat && isBackCut)
                {
                    return false;
                }

                // 일반 모자: BackCut 귀는 North에서만 허용
                if (isBackCut)
                {
                    return rot == Rot4.North;
                }

                // 일반 모자: North에서는 기본 귀 숨김
                if (rot == Rot4.North)
                {
                    return false;
                }

                // East/West 한쪽 귀 숨김
                // 단, 침대에 누워있을 때는 반대쪽 귀 숨김 적용 안 함
                if (!inBed && rot == Rot4.East && isRight)
                {
                    return false;
                }

                if (!inBed && rot == Rot4.West && isLeft)
                {
                    return false;
                }

                return true;
            }
            catch (System.Exception ex)
            {
                Log.Warning("[LapinRace] Ear patch error: " + ex);
                return true;
            }
        }
    }
}