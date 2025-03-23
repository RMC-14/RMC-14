using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Projectiles;

namespace Content.Shared._RMC14.Projectiles;

public sealed class GibOnHitSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _bodySystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GibOnHitComponent, ProjectileHitEvent>(OnTargetHit);
    }

    private void OnTargetHit(Entity<GibOnHitComponent> component, ref ProjectileHitEvent args)
    {
        if (!TryComp<BodyComponent>(args.Target, out var body))
            return;

        _bodySystem.GibBody(args.Target, true, body);
    }
}
