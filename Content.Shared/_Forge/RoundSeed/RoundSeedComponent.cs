using Robust.Shared.GameStates;

namespace Content.Shared._Forge.RoundSeed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRoundSeedSystem))]
public sealed partial class RoundSeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Seed;
}
