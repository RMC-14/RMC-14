using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Commendations;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCommendationSystem))]
public sealed partial class CommendationGiverComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Given;
}
