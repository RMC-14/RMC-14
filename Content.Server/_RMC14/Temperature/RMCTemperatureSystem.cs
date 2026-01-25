using Content.Server.Temperature.Components;
using Content.Server.Temperature.Systems;
using Content.Shared._RMC14.Temperature;

namespace Content.Server._RMC14.Temperature;

public sealed class RMCTemperatureSystem : SharedRMCTemperatureSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    public override float GetTemperature(EntityUid entity)
    {
        return CompOrNull<TemperatureComponent>(entity)?.CurrentTemperature ?? 0;
    }

    public override void ForceChangeTemperature(EntityUid entity, float temperature)
    {
        _temperature.ForceChangeTemperature(entity, temperature);
    }

    public override bool TryGetCurrentTemperature(EntityUid uid, out float temperature)
    {
        if (!TryComp(uid, out TemperatureComponent? temperatureComp))
        {
            temperature = 0;
            return true;
        }

        temperature = temperatureComp.CurrentTemperature;
        return false;
    }
}
