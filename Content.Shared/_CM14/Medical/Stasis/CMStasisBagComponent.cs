using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Stasis;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMStasisBagSystem))]
public sealed partial class CMStasisBagComponent : Component
{
    // TODO CM14 make upstream metabolism modifiers not shit, this just makes metabolism 1000 times slower instead of stopping it
    [DataField, AutoNetworkedField]
    public int MetabolismMultiplier = 1000;
}
