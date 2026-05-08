using System.Linq;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class LapinBackstoryEnemyComponent : MapComponent
    {
        private const int CheckInterval = 2500;

        public LapinBackstoryEnemyComponent(Map map) : base(map)
        {
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Current.Game == null || Find.TickManager == null)
                return;

            if (Find.TickManager.TicksGame < CheckInterval)
                return;

            if (Find.TickManager.TicksGame % CheckInterval != 0)
                return;

            ApplyEnemies();
        }

        private void ApplyEnemies()
        {
            var pawns = map.mapPawns.AllPawnsSpawned
                .Where(IsValidLapin)
                .ToList();

            foreach (Pawn a in pawns)
            {
                foreach (Pawn b in pawns)
                {
                    if (a == b) continue;

                    if (AreIdeologicalEnemies(a, b))
                    {
                        EnsureEnemyRelation(a, b);
                        GiveMoodPenalty(a);
                    }
                }
            }
        }

        private static bool IsValidLapin(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.Spawned
                && pawn.def?.defName == "Lapin"
                && pawn.story != null;
        }

        private static bool AreIdeologicalEnemies(Pawn a, Pawn b)
        {
            bool aRev = HasBackstory(a, "Lapin_Revolutionary_Army");
            bool bRev = HasBackstory(b, "Lapin_Revolutionary_Army");

            bool aRoyal = HasBackstory(a, "Lapin_Royalist");
            bool bRoyal = HasBackstory(b, "Lapin_Royalist");

            return (aRev && bRoyal) || (aRoyal && bRev);
        }

        private static bool HasBackstory(Pawn pawn, string key)
        {
            if (pawn?.story == null) return false;

            return BackstoryMatches(pawn.story.Childhood, key)
                || BackstoryMatches(pawn.story.Adulthood, key);
        }

        private static bool BackstoryMatches(BackstoryDef backstory, string key)
        {
            if (backstory == null) return false;

            return backstory.defName == key
                || backstory.identifier == key;
        }

        private static void EnsureEnemyRelation(Pawn pawn, Pawn other)
        {
            if (pawn?.relations == null || other?.relations == null)
                return;

            PawnRelationDef rivalDef =
                DefDatabase<PawnRelationDef>.GetNamedSilentFail("LP_IdeologicalEnemy");

            if (rivalDef == null)
            {
                Log.Warning("[LapinRace] PawnRelationDef 'LP_IdeologicalEnemy' not found.");
                return;
            }

            if (!pawn.relations.DirectRelationExists(rivalDef, other))
            {
                pawn.relations.AddDirectRelation(rivalDef, other);
                Log.Message("[LapinRace] Added ideological enemy relation: "
                    + pawn.LabelShort + " -> " + other.LabelShort);
            }

            if (!other.relations.DirectRelationExists(rivalDef, pawn))
            {
                other.relations.AddDirectRelation(rivalDef, pawn);
                Log.Message("[LapinRace] Added ideological enemy relation: "
                    + other.LabelShort + " -> " + pawn.LabelShort);
            }
        }

        private static void GiveMoodPenalty(Pawn pawn)
        {
            if (pawn.needs?.mood?.thoughts?.memories == null)
                return;

            ThoughtDef moodDef =
                DefDatabase<ThoughtDef>.GetNamedSilentFail("LP_BackstoryFactionHatredMood");

            if (moodDef == null)
                return;

            var memories = pawn.needs.mood.thoughts.memories;

            if (memories.Memories.Any(m => m.def == moodDef))
                return;

            memories.TryGainMemory(moodDef);

            Log.Message("[LapinRace] Added faction hatred mood thought to "
                + pawn.LabelShort);
        }
    }
}