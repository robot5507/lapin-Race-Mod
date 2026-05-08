using RimWorld;
using Verse;

namespace LapinRace
{
    [DefOf]
    public static class LapinPawnKindDefOf
    {
        public static PawnKindDef LapinColonist;
        public static PawnKindDef LapinSoldier;
        public static PawnKindDef LapinNoble;
        public static PawnKindDef LapinNobleOfficer;
        public static PawnKindDef LapinNobleChild;
        public static PawnKindDef LapinSoldierVoltigeur;
        public static PawnKindDef LapinSoldierArtillery;
        public static PawnKindDef LapinMaid;

        static LapinPawnKindDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(LapinPawnKindDefOf));
        }
    }
}
