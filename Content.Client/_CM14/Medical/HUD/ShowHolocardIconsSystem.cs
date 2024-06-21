using System.Diagnostics.CodeAnalysis;
using Content.Client.Overlays;
using Content.Shared._CM14.Medical.HUD;
using Content.Shared._CM14.Medical.HUD.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Medical.HUD;

public sealed class ShowHolocardIconsSystem : EquipmentHudSystem<HolocardScannerComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Urgent = "UrgentHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Emergency = "EmergencyHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Xeno = "XenoHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Permadead = "PermaHolocardIcon";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolocardStateComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<HolocardStateComponent> entity, ref GetStatusIconsEvent args)
    {
        if (!IsActive)
            return;

        var holocardIcons = GetIcons(entity);

        args.StatusIcons.AddRange(holocardIcons);
    }

    public IReadOnlyList<StatusIconData> GetIcons(Entity<HolocardStateComponent> entity)
    {
        var icons = new List<StatusIconData>();
        if (TryGetHolocardData(entity.Comp.HolocardStatus, out var holocardData) && holocardData.HolocardIcon != null)
        {
            var holocardIconPrototype = _prototypes.Index<StatusIconPrototype>(holocardData.HolocardIcon);
            icons.Add(holocardIconPrototype);
        }
        return icons;
    }

    public bool TryGetHolocardData(HolocardStatus holocardStatus, out HolocardData data)
    {
        data = new HolocardData();
        switch (holocardStatus)
        {
            case HolocardStatus.None:
                data.HolocardIcon = null;
                data.Description = Loc.GetString("hc-none-description");
                break;
            case HolocardStatus.Urgent:
                data.HolocardIcon = Urgent;
                data.Description = Loc.GetString("hc-urgent-description");
                break;
            case HolocardStatus.Emergency:
                data.HolocardIcon = Emergency;
                data.Description = Loc.GetString("hc-emergency-description");
                break;
            case HolocardStatus.Xeno:
                data.HolocardIcon = Xeno;
                data.Description = Loc.GetString("hc-xeno-description");
                break;
            case HolocardStatus.Permadead:
                data.HolocardIcon = Permadead;
                data.Description = Loc.GetString("hc-permadead-description");
                break;
            default:
                data = default;
                return false;
        }

        return true;
    }

    public bool TryGetHolocardName(HolocardStatus holocardStatus, [NotNullWhen(true)] out string? holocardName)
    {
        holocardName = null;
        switch (holocardStatus)
        {
            case HolocardStatus.None:
                holocardName = Loc.GetString("hc-none-name");
                break;
            case HolocardStatus.Urgent:
                holocardName = Loc.GetString("hc-urgent-name");
                break;
            case HolocardStatus.Emergency:
                holocardName = Loc.GetString("hc-emergency-name");
                break;
            case HolocardStatus.Xeno:
                holocardName = Loc.GetString("hc-xeno-name");
                break;
            case HolocardStatus.Permadead:
                holocardName = Loc.GetString("hc-permadead-name");
                break;
            default:
                return false;
        }
        return true;
    }

    public bool TryGetDescription(Entity<HolocardStateComponent> entity, [NotNullWhen(true)] out string? description)
    {
        description = null;
        if (TryGetHolocardData(entity.Comp.HolocardStatus, out var holocardData))
        {
            description = holocardData.Description;
            return true;
        }
        return false;
    }
}
