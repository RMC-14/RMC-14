using System.Numerics;
using Content.Client._RMC14.Sprite;
using Content.Shared._RMC14.Jittering;
using Content.Shared.Jittering;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client._RMC14.Jittering;

public sealed class RMCJitterinessVisualsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<JitteringComponent> _jitteringQuery;

    public override void Initialize()
    {
        base.Initialize();

        _jitteringQuery = GetEntityQuery<JitteringComponent>();

        SubscribeLocalEvent<RMCJitterinessComponent, ComponentStartup>(OnJitterinessStartup);
        SubscribeLocalEvent<RMCJitterinessComponent, ComponentShutdown>(OnJitterinessShutdown);
        SubscribeLocalEvent<JitteringComponent, ComponentInit>(OnJitteringInit);

        UpdatesAfter.Add(typeof(RMCSpriteVisualizerSystem));
    }

    private void OnJitterinessStartup(Entity<RMCJitterinessComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<RMCJitterinessVisualsComponent>(ent);
    }

    private void OnJitterinessShutdown(Entity<RMCJitterinessComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent) || !TryComp(ent, out RMCJitterinessVisualsComponent? visuals))
            return;

        RemoveShake((ent, visuals));
        RemCompDeferred<RMCJitterinessVisualsComponent>(ent);
    }

    private void OnJitteringInit(Entity<JitteringComponent> ent, ref ComponentInit args)
    {
        if (TryComp(ent, out RMCJitterinessVisualsComponent? visuals))
            RemoveShake((ent, visuals));
    }

    private void RemoveShake(Entity<RMCJitterinessVisualsComponent> ent)
    {
        var visuals = ent.Comp;
        if (TryComp(ent, out SpriteComponent? sprite) && sprite.Offset == visuals.Expected)
            _sprite.SetOffset((ent, sprite), sprite.Offset - visuals.Applied);

        visuals.Applied = Vector2.Zero;
        visuals.Expected = sprite?.Offset ?? Vector2.Zero;
        visuals.From = Vector2.Zero;
        visuals.To = Vector2.Zero;
        visuals.Progress = 0;
    }

    private Vector2 GetShake(int amplitude)
    {
        var y = amplitude / 3;
        return new Vector2(_random.Next(-amplitude, amplitude + 1), _random.Next(-y, y + 1)) / EyeManager.PixelsPerMeter;
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var query = EntityQueryEnumerator<RMCJitterinessVisualsComponent, RMCJitterinessComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var visuals, out var jitter, out var sprite))
        {
            if (_jitteringQuery.HasComp(uid))
                continue;

            visuals.Progress += frameTime / (float) jitter.ShakeEvery.TotalSeconds;
            if (visuals.Progress >= 1)
            {
                visuals.Progress = 0;
                visuals.From = visuals.To;
                visuals.To = GetShake(jitter.Amplitude);
            }

            var shake = Vector2.Lerp(visuals.From, visuals.To, visuals.Progress);
            if (shake == visuals.Applied && sprite.Offset == visuals.Expected)
                continue;

            var baseOffset = sprite.Offset == visuals.Expected ? sprite.Offset - visuals.Applied : sprite.Offset;
            visuals.Applied = shake;
            visuals.Expected = baseOffset + shake;
            _sprite.SetOffset((uid, sprite), visuals.Expected);
        }
    }
}
