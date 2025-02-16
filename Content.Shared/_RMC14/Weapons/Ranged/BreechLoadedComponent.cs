using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BreechLoadedSystem))]
public sealed partial class BreechLoadedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Open;

    /// <summary>
    /// If this is set to true, the user must open and close the breech between every shot.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NeedOpenClose;

    [DataField, AutoNetworkedField]
    public bool Ready;

    [DataField, AutoNetworkedField]
    public SoundSpecifier OpenSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Breech/ugl_open.ogg", AudioParams.Default.WithVolume(-6.5f));

    [DataField, AutoNetworkedField]
    public SoundSpecifier CloseSound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Breech/ugl_close.ogg", AudioParams.Default.WithVolume(-6.5f));

    [DataField, AutoNetworkedField]
    public bool ShowBreechOpen = true;
}

[Serializable, NetSerializable]
public enum BreechVisuals : byte
{
    Open
}
