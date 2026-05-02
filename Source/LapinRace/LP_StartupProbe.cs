using Verse;

namespace LapinRace
{
    [StaticConstructorOnStartup]
    public static class LP_StartupProbe
    {
        static LP_StartupProbe()
        {
            Log.Message("[LapinRace] LP_StartupProbe loaded");
        }
    }
}
