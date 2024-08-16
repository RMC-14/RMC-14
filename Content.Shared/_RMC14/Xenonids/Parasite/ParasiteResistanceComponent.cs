using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class ParasiteResistanceComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Probability = 0.5f;
}
