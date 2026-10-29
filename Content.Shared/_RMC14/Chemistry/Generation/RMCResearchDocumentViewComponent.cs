namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RMCResearchDocumentViewComponent : Component
{
    [DataField]
    public EntityUid? PendingViewer;

    [DataField, AutoPausedField]
    public TimeSpan OpenAt;

    [DataField]
    public TimeSpan OpenDelay = TimeSpan.FromSeconds(0.1);
}
