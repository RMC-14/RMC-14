using System.Collections.Generic;
using System.Numerics;
using Content.Shared.Construction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared._RMC14.Effects;
using Content.Shared._RMC14.Tools;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Spawners;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Repairable;

public sealed class RMCWeldEffectSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;

    public static readonly EntProtoId WeldEffect = "RMCEffectWeldingSparks";
    private static readonly Vector2 DefaultStartOffset = new(0f, -0.45f);
    private static readonly Vector2 DefaultEndOffset = new(0f, 0.45f);

    public override void Initialize()
    {
        SubscribeLocalEvent<WelderComponent, RMCToolUseEvent>(OnWelderToolUse, after: new[] { typeof(RMCToolSystem) });
        SubscribeLocalEvent<WeldableComponent, RMCToolDoAfterEvent>(OnWeldableToolDoAfter);
        SubscribeLocalEvent<WeldFinishedEvent>(OnWeldFinished);
        SubscribeLocalEvent<RMCRepairableDoAfterEvent>(OnRepairDoAfter, before: new[] { typeof(RMCRepairableSystem) });
        SubscribeLocalEvent<RMCWeldEffectSourceComponent, ConstructionInteractDoAfterEvent>(OnConstructionDoAfter);
    }

    private void OnWelderToolUse(Entity<WelderComponent> ent, ref RMCToolUseEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!HasComp<RMCWeldEffectSourceComponent>(target) &&
            !HasComp<WeldableComponent>(target))
            return;

        var duration = args.Delay;
        if (TryComp<ToolComponent>(ent, out var tool) && tool.SpeedModifier > 0)
            duration = TimeSpan.FromSeconds(duration.TotalSeconds / tool.SpeedModifier);

        var reverse = TryComp<WeldableComponent>(target, out var weldable) && weldable.IsWelded;
        SpawnWeldEffect(target, duration, reverse: reverse);
    }

    public void SpawnWeldEffect(EntityUid target, TimeSpan duration, bool reverse = false)
    {
        if (_net.IsClient)
            return;

        ClearActiveEffect(target);

        var xform = Transform(target);
        var offsetRotation = xform.LocalRotation;

        Vector2? startOffset = null;
        Vector2? endOffset = null;
        List<Vector2>? pathOffsets = null;

        if (TryComp<RMCWeldEffectSourceComponent>(target, out var source))
        {
            if (source.PathOffsets is { Count: > 1 })
            {
                pathOffsets = new List<Vector2>(source.PathOffsets);
                if (reverse)
                    pathOffsets.Reverse();

                startOffset = pathOffsets[0];
                endOffset = pathOffsets[^1];
            }
            else
            {
                startOffset = source.StartingOffset;
                endOffset = source.EndingOffset ?? -source.StartingOffset;

                if (reverse)
                    (startOffset, endOffset) = (endOffset, startOffset);
            }
        }
        else if (HasComp<WeldableComponent>(target))
        {
            startOffset = reverse ? DefaultEndOffset : DefaultStartOffset;
            endOffset = reverse ? DefaultStartOffset : DefaultEndOffset;
        }

        RotateOffsets(offsetRotation, ref startOffset, ref endOffset, ref pathOffsets);

        var effect = Spawn(WeldEffect, xform.Coordinates);
        if (!Exists(effect))
            return;

        var lifetime = MathF.Max(0.5f, (float) duration.TotalSeconds);
        var timed = EnsureComp<TimedDespawnComponent>(effect);
        timed.Lifetime = lifetime;

        if (startOffset != null)
        {
            var anim = EnsureComp<RMCSpriteOffsetAnimationComponent>(effect);
            anim.StartingOffset = startOffset.Value;
            anim.EndingOffset = endOffset;
            anim.PathOffsets = pathOffsets;
            anim.Length = lifetime;
            Dirty(effect, anim);
        }

        var active = EnsureComp<RMCWeldEffectActiveComponent>(target);
        active.Effect = effect;
    }

    private void OnWeldFinished(WeldFinishedEvent args)
    {
        if (args.Cancelled && args.Target is { } target)
            ClearActiveEffect(target);
    }

    private void OnWeldableToolDoAfter(EntityUid uid, WeldableComponent _, RMCToolDoAfterEvent args)
    {
        if (!args.Cancelled || args.WrappedEvent is not WeldFinishedEvent)
            return;

        ClearActiveEffect(uid);
    }

    private void OnRepairDoAfter(RMCRepairableDoAfterEvent args)
    {
        if (args.Cancelled && args.Target is { } target)
            ClearActiveEffect(target);
    }

    private void OnConstructionDoAfter(Entity<RMCWeldEffectSourceComponent> ent, ref ConstructionInteractDoAfterEvent args)
    {
        if (args.Cancelled)
            ClearActiveEffect(ent.Owner);
    }

    private void ClearActiveEffect(EntityUid target)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<RMCWeldEffectActiveComponent>(target, out var active))
            return;

        if (Exists(active.Effect))
            QueueDel(active.Effect);

        RemComp<RMCWeldEffectActiveComponent>(target);
    }

    private void RotateOffsets(
        Angle rotation,
        ref Vector2? startOffset,
        ref Vector2? endOffset,
        ref List<Vector2>? pathOffsets)
    {
        if (rotation == Angle.Zero)
            return;

        if (startOffset is { } start)
            startOffset = rotation.RotateVec(start);

        if (endOffset is { } end)
            endOffset = rotation.RotateVec(end);

        if (pathOffsets is not { Count: > 0 })
            return;

        for (var i = 0; i < pathOffsets.Count; i++)
        {
            pathOffsets[i] = rotation.RotateVec(pathOffsets[i]);
        }
    }
}
