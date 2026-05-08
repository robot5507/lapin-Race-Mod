using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace LapinRace
{
    public class LapinSiegeShellSupplyComponent : MapComponent
    {
        private const int CheckInterval = 2500;
        private HashSet<int> suppliedMortars = new HashSet<int>();

        public LapinSiegeShellSupplyComponent(Map map) : base(map) { }

        public override void MapComponentTick()
        {
            base.MapComponentTick();

            if (Find.TickManager.TicksGame % CheckInterval != 0)
                return;

            SupplyShellsForMortars();
        }

        private void SupplyShellsForMortars()
        {
            var mortars = map.listerThings.AllThings
                .Where(t => t.def.defName == "Turret_Mortar")
                .Where(t => t.Faction != null)
                .Where(t => !t.Faction.IsPlayer)
                .ToList();

            foreach (Thing mortar in mortars)
            {
                if (suppliedMortars.Contains(mortar.thingIDNumber))
                    continue;

                // 🔥 라핀 팩션 여부 확인
                bool isLapinFaction = map.mapPawns.AllPawnsSpawned.Any(p =>
                    p.Faction == mortar.Faction &&
                    p.def.defName == "Lapin");

                if (!isLapinFaction)
                    continue;

                Thing shells = ThingMaker.MakeThing(ThingDef.Named("Shell_HighExplosive"));
                shells.stackCount = 20;

                GenPlace.TryPlaceThing(shells, mortar.Position, map, ThingPlaceMode.Near);

                suppliedMortars.Add(mortar.thingIDNumber);

                Log.Message("[LapinRace] Supplied shells for Lapin mortar.");
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref suppliedMortars, "suppliedMortars", LookMode.Value);

            if (Scribe.mode == LoadSaveMode.PostLoadInit && suppliedMortars == null)
            {
                suppliedMortars = new HashSet<int>();
            }
        }
    }
}