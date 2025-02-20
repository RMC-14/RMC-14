using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Ladder;
using Content.Shared._RMC14.Map;
using Content.Shared.Construction.Components;
using Content.Shared.Coordinates;
using Content.Shared.Doors.Components;
using Content.Shared.Maps;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Construction;

public sealed class RMCTippingSystem : EntitySystem
{
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCConstructionAttemptEvent>(OnConstructionAttempt);

        SubscribeLocalEvent<DropshipComponent, DropshipMapInitEvent>(OnDropshipMapInit);

        SubscribeLocalEvent<RMCDropshipBlockedComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<RMCDropshipBlockedComponent, AnchorAttemptEvent>(OnAnchorAttempt);
        SubscribeLocalEvent<RMCDropshipBlockedComponent, UserAnchoredEvent>(OnUserAnchored);
    }
    
 private void OnXenoMapInit(Entity<XenoComponent> xeno, ref MapInitEvent args)
    {
        foreach (var actionId in xeno.Comp.ActionIds)
        {
            if (!xeno.Comp.Actions.ContainsKey(actionId) &&
                _action.AddAction(xeno, actionId) is { } newAction)
            {
                xeno.Comp.Actions[actionId] = newAction;
            }
        }

        xeno.Comp.NextRegenTime = _timing.CurTime + xeno.Comp.RegenCooldown;
        Dirty(xeno);

        if (!MathHelper.CloseTo(_xenoSpeedMultiplier, 1))
            _movementSpeed.RefreshMovementSpeedModifiers(xeno);
    }
  
