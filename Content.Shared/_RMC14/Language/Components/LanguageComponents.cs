using Content.Shared._RMC14.Language.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Language.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class LanguageComponent : Component
{
    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> SpokenLanguages = new();

    [DataField]
    public HashSet<ProtoId<LanguagePrototype>> UnderstoodLanguages = new();

    [DataField]
    public ProtoId<LanguagePrototype>? CurrentLanguage;

    [DataField]
    public ProtoId<LanguagePrototype>? DefaultLanguage;

    [Serializable, NetSerializable]
    public sealed class State : ComponentState
    {
        public ProtoId<LanguagePrototype>? CurrentLanguage;
        public List<ProtoId<LanguagePrototype>> SpokenLanguages;
        public List<ProtoId<LanguagePrototype>> UnderstoodLanguages;

        public State(ProtoId<LanguagePrototype>? currentLanguage, List<ProtoId<LanguagePrototype>> spokenLanguages, List<ProtoId<LanguagePrototype>> understoodLanguages)
        {
            CurrentLanguage = currentLanguage;
            SpokenLanguages = spokenLanguages;
            UnderstoodLanguages = understoodLanguages;
        }
    }
}
