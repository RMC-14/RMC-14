using Content.Shared._RMC14.Rules;

namespace Content.Client._RMC14.Rules;

public sealed class RMCRoundInfoSystem : EntitySystem
{
    private RMCRoundInfoComponent? GetRoundInfo()
    {
        var query = EntityQueryEnumerator<RMCRoundInfoComponent>();
        return query.MoveNext(out _, out var info) ? info : null;
    }

    public string GetOperationName() => GetRoundInfo()?.OperationName ?? string.Empty;
    public string GetPlanetName() => GetRoundInfo()?.PlanetName ?? string.Empty;
    public string GetShipName() => GetRoundInfo()?.ShipName ?? string.Empty;
}
