using Content.Shared.Sound.Components;

namespace Content.Shared._RMC14.Sound;

/// <summary>
/// RMC Version of the EmitSoundComponent, despawns instantly
/// </summary>
[RegisterComponent]
public sealed partial class RMCEmitSoundOnSpawnComponent : BaseEmitSoundComponent
{
    [DataField]
    public EntityUid? Entity;
}
