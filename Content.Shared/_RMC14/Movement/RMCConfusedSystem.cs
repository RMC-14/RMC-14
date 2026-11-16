using System.Numerics;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Movement;

public sealed class RMCConfusedSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private const uint RerollTicks = 30;

    private static readonly Vector2[] CardinalDirections =
    [
        new(0, 1),
        new(0, -1),
        new(1, 0),
        new(-1, 0),
    ];

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCConfusedComponent, ConfusedMovementEvent>(OnConfusedMovement);

        SubscribeLocalEvent<RMCConfusedStatusEffectComponent, StatusEffectAppliedEvent>(OnConfusedApplied);
        SubscribeLocalEvent<RMCConfusedStatusEffectComponent, StatusEffectRemovedEvent>(OnConfusedRemoved);
    }

    private void OnConfusedApplied(Entity<RMCConfusedStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        EnsureComp<RMCConfusedComponent>(args.Target);
        _popup.PopupEntity(Loc.GetString("rmc-confused-applied"), args.Target, args.Target);
    }

    private void OnConfusedRemoved(Entity<RMCConfusedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RemComp<RMCConfusedComponent>(args.Target);
        _popup.PopupEntity(Loc.GetString("rmc-confused-removed"), args.Target, args.Target);
    }

    private void OnConfusedMovement(Entity<RMCConfusedComponent> ent, ref ConfusedMovementEvent args)
    {
        if (args.WishDir == Vector2.Zero)
            return;

        var bucket = _timing.CurTick.Value / RerollTicks;
        var direction = CardinalDirections[PickDirection(GetNetEntity(ent.Owner).Id, bucket)];

        args.WishDir = direction * args.WishDir.Length();
    }

    private static int PickDirection(int netId, uint bucket)
    {
        unchecked
        {
            var hash = (uint) netId * 0x9E3779B1u ^ bucket * 0x85EBCA6Bu;
            hash ^= hash >> 16;
            hash *= 0x7FEB352Du;
            hash ^= hash >> 15;
            return (int) (hash % (uint) CardinalDirections.Length);
        }
    }
}
