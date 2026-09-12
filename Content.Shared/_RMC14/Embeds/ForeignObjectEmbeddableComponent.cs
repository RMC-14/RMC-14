using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Embeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedForeignObjectEmbeddedSystem))]
public sealed partial class ForeignObjectEmbeddableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float EmbedChance = 0.25f;

    [DataField, AutoNetworkedField]
    public bool RandomizeBodyPart = true;

}
