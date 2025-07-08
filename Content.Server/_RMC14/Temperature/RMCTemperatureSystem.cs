using Content.Server.Temperature.Components;
using Content.Shared._RMC14.Temperature;

namespace Content.Server._RMC14.Temperature;

public sealed class RMCTemperatureSystem : SharedRMCTemperatureSystem
{
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
