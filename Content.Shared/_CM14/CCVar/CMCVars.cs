using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._CM14.CCVar;

[CVarDefs]
public sealed class CMCVars : CVars
{
    public static readonly CVarDef<float> CMXenoDamageDealtMultiplier =
        CVarDef.Create("cm.xeno_damage_dealt_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoDamageReceivedMultiplier =
        CVarDef.Create("cm.xeno_damage_received_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoSpeedMultiplier =
        CVarDef.Create("cm.xeno_speed_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);
}
