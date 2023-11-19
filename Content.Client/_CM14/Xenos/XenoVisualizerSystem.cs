using Content.Shared._CM14.Xenos;
using Content.Shared._CM14.Xenos.Movement;
using Content.Shared._CM14.Xenos.Rest;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Throwing;
using Robust.Client.GameObjects;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;
using XenoComponent = Content.Shared._CM14.Xenos.XenoComponent;

namespace Content.Client._CM14.Xenos;

public sealed class XenoVisualizerSystem : VisualizerSystem<XenoComponent>
{
    [Dependency] private readonly MobStateSystem _mobState = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoComponent, GetDrawDepthEvent>(OnXenoGetDrawDepth);
    }

    private void OnXenoGetDrawDepth(Entity<XenoComponent> ent, ref GetDrawDepthEvent args)
    {
        if (_mobState.IsDead(ent))
        {
            if (args.DrawDepth > DrawDepth.DeadMobs)
            {
                args.DrawDepth = DrawDepth.DeadMobs;
            }
        }
    }

    protected override void OnAppearanceChange(EntityUid uid, XenoComponent component, ref AppearanceChangeEvent args)
    {
        var sprite = args.Sprite;

        if (sprite is not { BaseRSI: { } rsi } ||
            !sprite.LayerMapTryGet(XenoVisualLayers.Base, out var layer))
        {
            return;
        }

        var state = CompOrNull<MobStateComponent>(uid)?.CurrentState;

        switch (state)
        {
            case MobState.Critical:
                if (rsi.TryGetState("crit", out _))
                    sprite.LayerSetState(layer, "crit");
                break;
            case MobState.Dead:
                if (rsi.TryGetState("dead", out _))
                    sprite.LayerSetState(layer, "dead");
                break;
            default:
                if (args.AppearanceData.TryGetValue(XenoVisualLayers.Base, out var resting) &&
                    resting is XenoRestState.Resting)
                {
                    if (rsi.TryGetState("sleeping", out _))
                        sprite.LayerSetState(layer, "sleeping");
                    break;
                }

                if (rsi.TryGetState("alive", out _))
                    sprite.LayerSetState(layer, "alive");
                break;
        }

        UpdateDrawDepth((uid, sprite));
    }

    public void UpdateSprite(Entity<SpriteComponent?, MobStateComponent?, AppearanceComponent?, InputMoverComponent?, ThrownItemComponent?> entity)
    {
        var (_, sprite, mobState, appearance, input, thrown) = entity;
        if (!Resolve(entity, ref sprite, ref appearance))
            return;

        var state = MobState.Alive;
        if (Resolve(entity, ref mobState, false))
        {
            state = mobState.CurrentState;
        }

        Resolve(entity, ref input, ref thrown, false);

        if (sprite is not { BaseRSI: { } rsi } ||
            !sprite.LayerMapTryGet(XenoVisualLayers.Base, out var layer))
        {
            return;
        }

        // TODO CM14 split this up into multiple systems with ordered event subscription
        switch (state)
        {
            case MobState.Critical:
                if (rsi.TryGetState("crit", out _))
                    sprite.LayerSetState(layer, "crit");
                break;
            case MobState.Dead:
                if (rsi.TryGetState("dead", out _))
                    sprite.LayerSetState(layer, "dead");
                break;
            default:
                if (AppearanceSystem.TryGetData(entity, XenoVisualLayers.Base, out XenoRestState resting, appearance) &&
                    resting == XenoRestState.Resting)
                {
                    if (rsi.TryGetState("sleeping", out _))
                        sprite.LayerSetState(layer, "sleeping");
                    break;
                }

                if (thrown != null &&
                    rsi.TryGetState("thrown", out _))
                {
                    sprite.LayerSetState(layer, "thrown");
                    break;
                }

                if (input?.HeldMoveButtons > MoveButtons.None &&
                    rsi.TryGetState("running", out _))
                {
                    sprite.LayerSetState(layer, "running");
                    break;
                }

                if (rsi.TryGetState("alive", out _))
                    sprite.LayerSetState(layer, "alive");
                break;
        }
    }

    public void UpdateDrawDepth(Entity<SpriteComponent?> xeno)
    {
        if (!Resolve(xeno, ref xeno.Comp))
            return;

        var ev = new GetDrawDepthEvent(DrawDepth.Mobs);
        RaiseLocalEvent(xeno, ref ev);

        xeno.Comp.DrawDepth = (int) ev.DrawDepth;
    }

    public override void FrameUpdate(float frameTime)
    {
        var query = EntityQueryEnumerator<XenoAnimateMovementComponent, InputMoverComponent, MobStateComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var input, out var mobState, out var sprite))
        {
            if (mobState.CurrentState == MobState.Alive)
            {
                UpdateSprite((uid, sprite, mobState, null, input, null));
            }
        }
    }
}
