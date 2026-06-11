using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Emote;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCEmoteSystem))]
public sealed partial class EmoteBindingComponent : Component
{
    [DataField, AutoNetworkedField]
    public string[] HumanoidEmoteTexts = ["*screams!!","*yawns","*claps","*laughs","*cries","*salutes","*meows","*mews"];

    [DataField, AutoNetworkedField]
    public string[] XenoEmoteTexts = ["*roars!!","*hisses","*growls","*cries for help","*roars!!","*hisses","*growls","*cries for help"];
}
