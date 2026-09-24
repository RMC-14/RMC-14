using Content.Client._RMC14.Weapons.Ranged.Prediction;
using Content.Shared._RMC14.Xenonids.Projectile;
using Robust.Client.GameObjects;
using Robust.Client.Physics;
using Robust.Client.Player;

namespace Content.Client._RMC14.Xenonids.Projectile;

public sealed class ClientXenoProjectileSystem : EntitySystem
{
    [Dependency] private readonly GunPredictionSystem _gunPrediction = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoProjectileShotComponent, ComponentStartup>(OnShotStartup);

        SubscribeLocalEvent<XenoClientProjectileShotComponent, UpdateIsPredictedEvent>(OnUpdateIsPredicted);
    }

    private void OnShotStartup(Entity<XenoProjectileShotComponent> ent, ref ComponentStartup args)
    {
        if (!_gunPrediction.GunPrediction)
            return;

        if (ent.Comp.ShooterEnt != _player.LocalEntity)
            return;

        _sprite.SetVisible(ent.Owner, false);
    }

    private void OnUpdateIsPredicted(Entity<XenoClientProjectileShotComponent> ent, ref UpdateIsPredictedEvent args)
    {
        args.IsPredicted = true;
    }
}
