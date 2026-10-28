namespace Content.Shared._RMC14.Botany;

[RegisterComponent]
public sealed partial class RMCBotanySwabComponent : Component
{
    [DataField]
    public float SwabDelay = 2f;

    public List<IComponent>? CapturedComponents;
}
