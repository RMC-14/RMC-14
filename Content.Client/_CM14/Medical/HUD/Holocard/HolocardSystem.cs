
using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared._CM14.Medical.Systems;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD.Holocard;

public sealed class HolocardSystem : SharedHolocardSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public IReadOnlyList<StatusIconData> GetIcons(Entity<HolocardStateComponent> entity)
    {
        var icons = new List<StatusIconData>();
        if (TryGetHolocardData(entity.Comp.HolocardStaus, out var holocardData) && holocardData.HolocardIconPrototype is not null)
        {
            var holocardIconPrototype = _prototype.Index<StatusIconPrototype>(holocardData.HolocardIconPrototype);
            icons.Add(holocardIconPrototype);
        }
        return icons;
    }

    public bool TryGetDescription(Entity<HolocardStateComponent> entity, [NotNullWhen(true)] out string? description)
    {
        description = null;
        if (TryGetHolocardData(entity.Comp.HolocardStaus, out var holocardData))
        {
            description = holocardData.Description;
            return true;
        }
        return false;
    }
}
