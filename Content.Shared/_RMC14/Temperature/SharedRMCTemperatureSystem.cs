namespace Content.Shared._RMC14.Temperature;

public abstract class SharedRMCTemperatureSystem : EntitySystem
{
    public virtual float GetTemperature(EntityUid entity)
    {
        return 0;
    }

    public virtual void ForceChangeTemperature(EntityUid entity, float temperature)
    {
    }
}
