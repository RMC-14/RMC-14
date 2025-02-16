using Robust.Shared.GameStates;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Weapons.Ranged.Prediction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PredictedProjectileServerComponent : Component
{
    public ICommonSession? Shooter;

    [DataField, AutoNetworkedField]
    public int ClientId;

    [DataField, AutoNetworkedField]
    public EntityUid? ClientEnt;

    [DataField]
    public bool Hit;
}
