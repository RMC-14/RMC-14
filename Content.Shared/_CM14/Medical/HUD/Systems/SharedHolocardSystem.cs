
using System.Diagnostics.CodeAnalysis;
using Content.Shared._CM14.Medical.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Medical.Systems;

public abstract class SharedHolocardSystem : EntitySystem
{
    [ValidatePrototypeId<HolocardPrototype>]
    private const string None = "NoneHolocard";

    [ValidatePrototypeId<HolocardPrototype>]
    private const string Urgent = "UrgentHolocard";

    [ValidatePrototypeId<HolocardPrototype>]
    private const string Emergency = "EmergencyHolocard";

    [ValidatePrototypeId<HolocardPrototype>]
    private const string Xeno = "PermaHolocard";

    [ValidatePrototypeId<HolocardPrototype>]
    private const string Permadead = "XenoHolocard";
    public bool TryGetHolocardPrototypeFromStatus(HolocardStaus holocardStaus, [NotNullWhen(true)] out string? protoId)
    {
        protoId = null;
        switch (holocardStaus)
        {
            case HolocardStaus.None:
                protoId = None;
                break;
            case HolocardStaus.Urgent:
                protoId = Urgent;
                break;
            case HolocardStaus.Emergency:
                protoId = Emergency;
                break;
            case HolocardStaus.Xeno:
                protoId = Xeno;
                break;
            case HolocardStaus.Permadead:
                protoId = Permadead;
                break;
            default:
                return false;
        }
        return true;
    }
}
