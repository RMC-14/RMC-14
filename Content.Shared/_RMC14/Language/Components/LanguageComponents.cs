using Content.Shared._RMC14.Language.Prototypes;
using Content.Shared._RMC14.Language.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedLanguageLearningSystem), typeof(SharedLanguageSystem))]
public sealed partial class LanguageComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();

    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? CurrentLanguage;

    [DataField, AutoNetworkedField]
    public ProtoId<LanguagePrototype>? DefaultLanguage;

    [DataField, AutoNetworkedField]
    public EntProtoId<LanguagePresetComponent>? Preset;
}
