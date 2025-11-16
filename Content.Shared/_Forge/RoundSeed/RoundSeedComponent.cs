using Robust.Shared.GameStates;

namespace Content.Shared._NC14.RoundSeed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedNCRoundSeedSystem))]
public sealed partial class NCRoundSeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Seed;
}
