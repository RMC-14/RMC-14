using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using RmcDrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._RMC14.Vehicle;

public sealed class VehicleSqueezeUnderVisualSystem : EntitySystem
{
    private const float SqueezingAlpha = 0.4f;

    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VehicleSqueezingUnderComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<VehicleSqueezingUnderComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<VehicleSqueezingUnderComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent.Owner, out SpriteComponent? sprite))
            return;

        _sprite.SetDrawDepth((ent.Owner, sprite), (int) RmcDrawDepth.BelowMobs);
        _sprite.SetColor((ent.Owner, sprite), sprite.Color.WithAlpha(SqueezingAlpha));
    }

    private void OnShutdown(Entity<VehicleSqueezingUnderComponent> ent, ref ComponentShutdown args)
    {
        if (MetaData(ent.Owner).EntityLifeStage >= EntityLifeStage.Terminating ||
            !TryComp(ent.Owner, out SpriteComponent? sprite))
        {
            return;
        }

        _sprite.SetDrawDepth((ent.Owner, sprite), (int) RmcDrawDepth.Mobs);
        _sprite.SetColor((ent.Owner, sprite), sprite.Color.WithAlpha(1f));
    }
}
