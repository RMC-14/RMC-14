using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Commendations;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAwardRecommendationSystem), typeof(SharedCommendationSystem))]
public sealed partial class RMCAwardRecommendationComponent : Component
{
    /// <summary>
    /// How many recommendations this entity can give out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int MaxRecommendations = 2;

    /// <summary>
    /// How many recommendations this entity has given out.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int RecommendationsGiven;

    /// <summary>
    /// Whether this entity can recommend awards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanRecommend = true;
}
