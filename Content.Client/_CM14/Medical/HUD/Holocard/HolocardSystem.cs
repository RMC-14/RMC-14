
using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Medical.Components;
using Content.Shared._CM14.Medical.Prototypes;
using Content.Shared._CM14.Medical.Systems;
using Content.Shared.Damage;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD.Holocard;

public sealed class HolocardSystem : SharedHolocardSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public IReadOnlyList<StatusIconData> GetIcons(Entity<HolocardComponent> entity)
    {
        var icons = new List<StatusIconData>();
        if (TryGetHolocardPrototypeFromStatus(entity.Comp.HolocardStaus, out var holocardProtoid))
        {
            var holocardPrototype = _prototype.Index<HolocardPrototype>(holocardProtoid);
            if (holocardPrototype.HolocardIcon is StatusIconPrototype holocardIconPrototype)
            {
                icons.Add(holocardIconPrototype);
            }
        }
        return icons;
    }

    public bool TryGetDescription(Entity<HolocardComponent> entity, [NotNullWhen(true)] out string? description)
    {
        description = null;
        if (TryGetHolocardPrototypeFromStatus(entity.Comp.HolocardStaus, out var holocardProtoid))
        {
            var holocardPrototype = _prototype.Index<HolocardPrototype>(holocardProtoid);
            description = holocardPrototype.Description;
            return true;
        }
        return false;
    }
}
