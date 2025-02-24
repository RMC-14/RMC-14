using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Coordinates;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Tools;

public sealed class RMCToolSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCRefinableComponent, ExaminedEvent>(OnRefinableExamined);
        SubscribeLocalEvent<RMCRefinableComponent, InteractUsingEvent>(OnRefinableInteractUsing);
        SubscribeLocalEvent<RMCRefinableComponent, RMCRefinableDoAfterEvent>(OnRefinableDoAfter);
        SubscribeLocalEvent<ToolComponent, RMCToolUseEvent>(OnToolUse);
    }

    private void OnRefinableExamined(Entity<RMCRefinableComponent> ent, ref ExaminedEvent args)
    {
        if (!_prototypes.TryIndex(ent.Comp.Tool, out var tool))
            return;

        var quality = Loc.GetString(tool.ToolName);
        using (args.PushGroup(nameof(RMCRefinableComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-refinable-can-be-refined", ("tool", quality)));
        }
    }

    private void OnRefinableInteractUsing(Entity<RMCRefinableComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (!HasComp<ToolComponent>(args.Used))
            return;

        args.Handled = true;
        if (ent.Comp.Amount > _stack.GetCount(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-refinable-not-enough", ("amount", ent.Comp.Amount), ("name", Name(ent))), ent, args.User);
            return;
        }

        var ev = new RMCRefinableDoAfterEvent();
        var delay = (float)ent.Comp.Delay.TotalSeconds;
        _tool.UseTool(args.Used, args.User, ent, delay, ent.Comp.Tool, ev, ent.Comp.Fuel);
    }

    private void OnRefinableDoAfter(Entity<RMCRefinableComponent> ent, ref RMCRefinableDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        if (HasComp<StackComponent>(ent))
        {
            if (!_stack.Use(ent, ent.Comp.Amount))
            {
                _popup.PopupClient(Loc.GetString("rmc-refinable-not-enough", ("amount", ent.Comp.Amount), ("name", Name(ent))), ent, args.User);
                return;
            }
        }
        else
        {
            if (_net.IsClient)
                return;

            QueueDel(ent);
        }

        if (_net.IsClient)
            return;

        var spawns = EntitySpawnCollection.GetSpawns(ent.Comp.Spawn);
        foreach (var spawn in spawns)
        {
            SpawnAtPosition(spawn, ent.Owner.ToCoordinates());
        }
    }

    /// <summary>
    ///     Reduce the DoAfter duration of the tool action based on it's skill specialisation.
    /// </summary>
    private void OnToolUse(Entity<ToolComponent> ent, ref RMCToolUseEvent args)
    {
        if (!TryComp(args.User, out SkillsComponent? skills) || args.Handled)
            return;

        args.Delay *= _skills.GetSkillDelayMultiplier(args.User, ent.Comp.Skill);
        args.Handled = true;
    }
}

/// <summary>
///     Raised on a tool when it's being used to possibly alter the delay of it's action.
/// </summary>
[ByRefEvent]
public record struct RMCToolUseEvent(EntityUid User, TimeSpan Delay, bool Handled = false);
