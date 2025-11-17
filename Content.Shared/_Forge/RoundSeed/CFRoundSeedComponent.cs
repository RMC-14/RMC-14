using Robust.Shared.GameStates;

namespace Content.Shared._Forge.RoundSeed;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CFSharedRoundSeedSystem))]
public sealed partial class CFRoundSeedComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Seed;
}
