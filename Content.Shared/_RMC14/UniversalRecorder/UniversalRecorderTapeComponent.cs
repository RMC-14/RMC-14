namespace Content.Shared._RMC14.UniversalRecorder;

[RegisterComponent]
public sealed partial class UniversalRecorderTapeComponent : Component
{
    [DataField]
    public TimeSpan MaxCapacity = TimeSpan.FromMinutes(20);

    [DataField]
    public TimeSpan RespoolTime = TimeSpan.FromSeconds(5);

    [DataField]
    public string ScrewdriverQuality = "Screwing";
}
