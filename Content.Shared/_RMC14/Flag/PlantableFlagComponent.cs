using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Flag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(PlantableFlagSystem))]
public sealed partial class PlantableFlagComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Delay = TimeSpan.FromSeconds(6);

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RaiseStartSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_raising.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RaiseEndSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_raised.ogg");

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RaisedCombatSound = null; // TODO RMC14

    [DataField, AutoNetworkedField]
    public SoundSpecifier? RaisedCombatAlliesSound = null; // TODO RMC14

    [DataField, AutoNetworkedField]
    public SoundSpecifier? LowerStartSound = new SoundPathSpecifier("/Audio/_RMC14/Effects/flag_lowering.ogg");

    [DataField, AutoNetworkedField]
    public int AlliesRequired = 14;

    [DataField, AutoNetworkedField]
    public int AlliesRange = 7;

    [DataField, AutoNetworkedField]
    public Vector2 DeployOffset = new(0, 0.5f);
}
