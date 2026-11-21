using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCTacticalReloadSlotComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string SlotId = string.Empty;

    [DataField]
    public TimeSpan TacticalReloadTime = TimeSpan.FromSeconds(1.25);

    [DataField, AutoNetworkedField]
    public SkillWhitelist TacticalSkills;

    [DataField]
    public LocId SwapText = "rmc-hypospray-swap-tacreload";

    [DataField]
    public LocId LoadText = "rmc-hypospray-load-tacreload";
}

[Serializable, NetSerializable]
public sealed partial class TacticalReloadDoAfterEvent : SimpleDoAfterEvent
{
}
