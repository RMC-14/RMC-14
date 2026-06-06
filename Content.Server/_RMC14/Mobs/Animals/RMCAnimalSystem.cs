using Content.Server._RMC14.NPC;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.Item;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Mobs.Animals;

public abstract partial class RMCAnimalSystem : EntitySystem
{
    [Dependency] protected readonly SharedContainerSystem Container = default!;
    [Dependency] protected readonly DamageableSystem Damageable = default!;
    [Dependency] protected readonly EntityLookupSystem Lookup = default!;
    [Dependency] protected readonly MobStateSystem MobState = default!;
    [Dependency] protected readonly NpcFactionSystem Faction = default!;
    [Dependency] protected readonly SharedPhysicsSystem Physics = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly RMCNPCSystem RMCNpc = default!;
    [Dependency] protected readonly IRobustRandom Random = default!;
    [Dependency] protected readonly SharedStunSystem Stun = default!;
    [Dependency] protected readonly TagSystem Tags = default!;
    [Dependency] protected readonly IGameTiming Timing = default!;
    [Dependency] protected readonly SharedTransformSystem Transform = default!;

    protected EntityQuery<ActorComponent> ActorQuery;
    protected EntityQuery<DamageableComponent> DamageableQuery;
    protected EntityQuery<FlammableComponent> FlammableQuery;
    protected EntityQuery<ItemComponent> ItemQuery;
    protected EntityQuery<MobStateComponent> MobQuery;
    protected EntityQuery<MobThresholdsComponent> ThresholdsQuery;
    protected EntityQuery<NpcFactionMemberComponent> FactionQuery;
    protected EntityQuery<PhysicsComponent> PhysicsQuery;
    protected EntityQuery<TransformComponent> XformQuery;

    public override void Initialize()
    {
        base.Initialize();

        ActorQuery = GetEntityQuery<ActorComponent>();
        DamageableQuery = GetEntityQuery<DamageableComponent>();
        FlammableQuery = GetEntityQuery<FlammableComponent>();
        ItemQuery = GetEntityQuery<ItemComponent>();
        MobQuery = GetEntityQuery<MobStateComponent>();
        ThresholdsQuery = GetEntityQuery<MobThresholdsComponent>();
        FactionQuery = GetEntityQuery<NpcFactionMemberComponent>();
        PhysicsQuery = GetEntityQuery<PhysicsComponent>();
        XformQuery = GetEntityQuery<TransformComponent>();
    }

}
