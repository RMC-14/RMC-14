using Content.Shared.Storage;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Crate;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CrateOpenableSystem))]
public sealed partial class CrateOpenableComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> Tool = "Prying";

    [DataField, AutoNetworkedField]
    public List<EntitySpawnEntry> Spawn = new()
    {
        new EntitySpawnEntry { PrototypeId = "CMSheetMetal2" },
    };

    [DataField, AutoNetworkedField]
    public LocId WrongToolPopup = "rmc-crate-openable-need-crowbar";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Structures/metalhit.ogg");
}
