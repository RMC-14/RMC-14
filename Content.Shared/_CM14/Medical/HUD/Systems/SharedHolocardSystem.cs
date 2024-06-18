
using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Medical;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.Systems;

public abstract class SharedHolocardSystem : EntitySystem
{
    [NetSerializable, Serializable]
    public enum HolocardChangeUIKey : byte
    {
        Key
    }

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Urgent = "UrgentHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Emergency = "EmergencyHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Xeno = "PermaHolocardIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string Permadead = "XenoHolocardIcon";
    public bool TryGetHolocardData(HolocardStaus holocardStaus, [NotNullWhen(true)] out HolocardData? holocardData)
    {
        holocardData = new();
        switch (holocardStaus)
        {
            case HolocardStaus.None:
                holocardData.HolocardIconPrototype = null;
                holocardData.Description = Loc.GetString("hc-none-description");
                break;
            case HolocardStaus.Urgent:
                holocardData.HolocardIconPrototype = Urgent;
                holocardData.Description = Loc.GetString("hc-urgent-description");
                break;
            case HolocardStaus.Emergency:
                holocardData.HolocardIconPrototype = Emergency;
                holocardData.Description = Loc.GetString("hc-emergency-description");
                break;
            case HolocardStaus.Xeno:
                holocardData.HolocardIconPrototype = Xeno;
                holocardData.Description = Loc.GetString("hc-xeno-description");
                break;
            case HolocardStaus.Permadead:
                holocardData.HolocardIconPrototype = Permadead;
                holocardData.Description = Loc.GetString("hc-permadead-description");
                break;
            default:
                holocardData = null;
                return false;
        }
        return true;
    }
}
