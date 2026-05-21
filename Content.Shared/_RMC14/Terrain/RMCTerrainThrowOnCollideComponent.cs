using Content.Shared.Whitelist;

namespace Content.Shared._RMC14.Terrain;

[RegisterComponent]
[Access(typeof(RMCTerrainThrowOnCollideSystem))]
public sealed partial class RMCTerrainThrowOnCollideComponent : Component
{
    [DataField]
    public float ThrowSpeed = 10;

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(0.5);

    [DataField]
    public Direction? Direction;

    [DataField]
    public EntityWhitelist? Whitelist;
}
