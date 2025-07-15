using Content.Shared.Atmos;
using Content.Shared.Temperature;

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

    public virtual bool TryGetCurrentTemperature(EntityUid uid, out float temperature)
    {
        // TODO RMC14
        temperature = TemperatureHelpers.CelsiusToKelvin(Atmospherics.NormalBodyTemperature);
        return true;
    }
}
