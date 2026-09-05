using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Embeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedForeignObjectEmbeddedSystem))]
public sealed partial class ShrapnelChanceProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SourceId = "foreign_object";

    [DataField, AutoNetworkedField]
    public float EmbedChance = 0.25f;

    [DataField, AutoNetworkedField]
    public int Count = 1;

    [DataField, AutoNetworkedField]
    public bool RandomizeBodyPart = true;

    //Todo: When body part targeting is implmented, extend this to target specifc parts.
}
