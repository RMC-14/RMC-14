using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.ParaDrop;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ParaDroppableComponent : Component
{
    [DataField, AutoNetworkedField]
    public float DropDuration = 3.5f;

    [DataField, AutoNetworkedField]
    public int DropScatter = 7;

    [DataField, AutoNetworkedField]
    public float FallHeight = 7;

    [DataField, AutoNetworkedField]
    public Vector2 ParachuteOffset = new(0, 0.75f);

    [DataField, AutoNetworkedField]
    public SoundSpecifier DropSound = new SoundPathSpecifier("/Audio/_RMC14/Items/fulton.ogg");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier ParachuteSprite =
        new SpriteSpecifier.Rsi(new ResPath("Objects/Tools/fulton_balloon.rsi"), "fulton_balloon");

    [DataField, AutoNetworkedField]
    public TimeSpan? LastParaDrop;
}
