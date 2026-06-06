using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Despoiler;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoDespoilerAcidSystem))]
public sealed partial class XenoDespoilerAcidTierComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Tier = 1;

    [DataField, AutoNetworkedField]
    public int MaxTier = 4;
}
