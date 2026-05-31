using RimWorld;
using Verse;

namespace LapinRace
{
    public class CompProperties_AutomatonPlayerControl : CompProperties
    {
        public CompProperties_AutomatonPlayerControl()
        {
            compClass = typeof(CompAutomatonPlayerControl);
        }
    }

    public class CompAutomatonPlayerControl : ThingComp
    {
        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);

            Pawn pawn = parent as Pawn;
            if (pawn == null)
            {
                return;
            }

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
            {
                return;
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }

            if (pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
            }
        }

        public override void CompTick()
        {
            base.CompTick();

            Pawn pawn = parent as Pawn;
            if (pawn == null || !pawn.Spawned)
            {
                return;
            }

            if (!pawn.IsHashIntervalTick(250))
            {
                return;
            }

            if (pawn.Faction == null || !pawn.Faction.IsPlayer)
            {
                return;
            }

            if (pawn.playerSettings == null)
            {
                pawn.playerSettings = new Pawn_PlayerSettings(pawn);
            }

            if (pawn.drafter == null)
            {
                pawn.drafter = new Pawn_DraftController(pawn);
            }
        }
    }
}