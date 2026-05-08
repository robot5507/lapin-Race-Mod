using System;
using RimWorld;

namespace ASEL
{
    // Token: 0x02000016 RID: 22
    public static class PawnGeneExtensions
    {
        // Token: 0x06000066 RID: 102 RVA: 0x00004C44 File Offset: 0x00002E44
        public static void RemoveAllGenes(this Pawn_GeneTracker geneTracker)
        {
            bool flag = geneTracker == null;
            if (!flag)
            {
                geneTracker.Endogenes.Clear();
                geneTracker.Xenogenes.Clear();
            }
        }
    }
}