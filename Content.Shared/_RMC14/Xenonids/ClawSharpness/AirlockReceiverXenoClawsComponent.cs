using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ClawSharpness;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoClawsSystem))]
public sealed partial class AirlockReceiverXenoClawsComponent : Component
{
    [DataField, AutoNetworkedField]
    public float MaxHealth = 500;

    [DataField, AutoNetworkedField]
    public int HitsToDestroyBolted = 10;

    [DataField, AutoNetworkedField]
    public int HitsToDestroyWelded = 15;

    [DataField, AutoNetworkedField]
    public XenoClawType MinimumClawStrength = XenoClawType.Sharp;
}
