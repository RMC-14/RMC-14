using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class ParasiteResistanceComponent : Component
{
    /// <summary>
    ///     The current amount of parasite leaps.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float Count = 0f;

    /// <summary>
    ///     How many parasite leaps it takes to get through the clothing.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxCount = 1f;
}