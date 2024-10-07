namespace Content.Server._RMC14.Stations;

public sealed class RMCStationJobsSystem : EntitySystem
{
    public int GetSlots(int marines, float factor, int c, int min, int max)
    {
        if (marines <= factor)
            return min;

        return (int) Math.Floor(Math.Clamp(marines / factor + c, min, max));
    }
}
