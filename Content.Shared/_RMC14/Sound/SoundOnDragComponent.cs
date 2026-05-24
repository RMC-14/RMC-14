using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._RMC14.Sound;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMSoundSystem))]
public sealed partial class SoundOnDragComponent : Component
{
    private float _dragSoundDistance;

    [DataField(required: true), AutoNetworkedField]
    public SoundSpecifier? Sound;

    //[DataField, AutoNetworkedField]
    //public EntityUid? Entity;

    /// <summary>
    ///     Used to keep track of how far we've been dragged before playing a sound.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float DragSoundDistance
    {
        get => _dragSoundDistance;
        set
        {
            if (MathHelper.CloseToPercent(_dragSoundDistance, value)) return;
            _dragSoundDistance = value;
        }
    }

    [ViewVariables(VVAccess.ReadWrite)]
    public EntityCoordinates LastPosition { get; set; }

    public TimeSpan LastSoundTime = TimeSpan.FromSeconds(0);
}
