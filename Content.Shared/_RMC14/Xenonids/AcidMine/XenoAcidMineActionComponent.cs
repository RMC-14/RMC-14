using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.AcidMine;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoAcidMineSystem))]
public sealed partial class XenoAcidMineActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public float FailCooldownMult = 0.5f;

    [DataField, AutoNetworkedField]
    public TimeSpan SuccessCooldown = TimeSpan.FromSeconds(6);
}
