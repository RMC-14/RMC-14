using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Shared._RMC14.Weapons.Ranged.IFF;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IFFToggleComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Enabled = true;

    //do not change during runtime
    [DataField, AutoNetworkedField]
    public bool RequireIDLock = false;

    //do not change during runtime
    [DataField, AutoNetworkedField]
    public bool ChangeStats = false;

    [DataField, AutoNetworkedField]
    public Dictionary<SelectiveFire, SelectiveFireModifierSet> IFFModifiers = new()
    {
        {SelectiveFire.SemiAuto, new SelectiveFireModifierSet(0.0f, 10.0, false, 2.0, 6)}
    };

    [DataField, AutoNetworkedField]
    public Dictionary<SelectiveFire, SelectiveFireModifierSet> BaseModifiers = new();

    [DataField, AutoNetworkedField]
    public SelectiveFire BaseFireModes = SelectiveFire.Invalid;

    [DataField, AutoNetworkedField]
    public SelectiveFire IFFFireModes = SelectiveFire.SemiAuto;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionID = "RMCActionToggleIFF";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi EnabledIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/iff_toggle_actions.rsi"), "iff_toggle_on");

    [DataField, AutoNetworkedField]
    public SpriteSpecifier.Rsi DisabledIcon = new SpriteSpecifier.Rsi(new ResPath("_RMC14/Actions/iff_toggle_actions.rsi"), "iff_toggle_off");

    [DataField, AutoNetworkedField]
    public SoundSpecifier ToggleSound = new SoundPathSpecifier("/Audio/_RMC14/Machines/click.ogg");
}
