using Content.Shared._RMC14.Xenonids.Leap;
using Content.Shared.Actions;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared._RMC14.Xenonids.Cruelty;

public sealed partial class XenoCrueltySystem : EntitySystem
{
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoCrueltyComponent, MeleeHitEvent>(OnCrueltyHit);
    }

    private void OnCrueltyHit(Entity<XenoCrueltyComponent> xeno, ref  MeleeHitEvent args)
    {
        bool hit = false;
        foreach (var ent in args.HitEntities)
        {
            if (!_xeno.CanAbilityAttackTarget(xeno, ent))
                continue;

            hit = true;
            break;
        }

        if (!hit)
            return;

        foreach (var (actionId, action) in _actions.GetActions(xeno))
        {
            if ((action.BaseEvent is XenoLeapActionEvent)
                && action.Cooldown != null)
            {
                var cooldownEnd = action.Cooldown.Value.End - xeno.Comp.CooldownReduction;
                if (cooldownEnd < action.Cooldown.Value.Start)
                    _actions.ClearCooldown(actionId);
                else
                    _actions.SetCooldown(actionId, action.Cooldown.Value.Start, cooldownEnd);
            }
        }
    }
}
