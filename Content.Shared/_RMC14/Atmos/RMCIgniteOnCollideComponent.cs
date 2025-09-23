using Content.Shared.Damage;
using Content.Shared.Physics;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class RMCIgniteOnCollideComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? MaxStacks;

    [DataField, AutoNetworkedField]
    public int Intensity = 15;

    [DataField, AutoNetworkedField]
    public int Duration = 55;

    [DataField, AutoNetworkedField]
    public bool InitDamaged;

    [DataField, AutoNetworkedField]
    public DamageSpecifier? TileDamage;

    [DataField, AutoNetworkedField]
    public double ArmorMultiplier = 1;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? ArmorWhitelist;

    [DataField, AutoNetworkedField]
    public CollisionGroup Collision = CollisionGroup.FullTileLayer;
}
