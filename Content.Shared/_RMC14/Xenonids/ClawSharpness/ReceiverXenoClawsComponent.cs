using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ClawSharpness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoClawsSystem))]
public sealed partial class ReceiverXenoClawsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxHealth = 100;

    [DataField, AutoNetworkedField]
    public int HitsToDestroy = 5;

    [DataField, AutoNetworkedField]
    public XenoClawType MinimumClawStrength = XenoClawType.Sharp;

    [DataField, AutoNetworkedField]
    public int? MinimumXenoTier = null;
}
