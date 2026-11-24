using Content.Shared._RMC14.Commendations;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Recommendation;

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
    /// List of LastPlayerIds that this entity has recommended.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> RecommendedLastPlayerIds = new();

    /// <summary>
    /// Whether this entity can recommend awards.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool CanRecommend = true;
}
