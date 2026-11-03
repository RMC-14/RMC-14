using System.Text;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs.Components;

namespace Content.Shared._RMC14.Chemistry.ReagentScanner;

public sealed class RMCReagentScannerSystem : EntitySystem
{
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCReagentScannerComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<RMCReagentScannerComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled ||
            !args.CanReach ||
            args.Target is not { } target ||
            HasComp<MobStateComponent>(target))
        {
            return;
        }

        args.Handled = true;
        var user = args.User;
        if (!TryComp(target, out SolutionContainerManagerComponent? manager))
        {
            _rmcChat.ChatMessageToOne($"No significant chemical agents found in {Name(target)}.", user);
            return;
        }

        var reagents = new Dictionary<string, FixedPoint2>();
        var total = FixedPoint2.Zero;
        foreach (var (_, solution) in _solution.EnumerateSolutions((target, manager)))
        {
            foreach (var reagent in solution.Comp.Solution.Contents)
            {
                var id = reagent.Reagent.Prototype;
                reagents[id] = reagents.GetValueOrDefault(id) + reagent.Quantity;
                total += reagent.Quantity;
            }
        }

        if (reagents.Count == 0 || total <= FixedPoint2.Zero)
        {
            _rmcChat.ChatMessageToOne($"No active chemical agents found in {Name(target)}.", user);
            return;
        }

        var msg = new StringBuilder("Chemicals found: ");
        foreach (var (id, quantity) in reagents)
        {
            var name = _rmcReagent.TryIndex(id, out var reagent) ? reagent.LocalizedName : id;
            msg.Append($"\n    {name}");

            if (ent.Comp.Details)
                msg.Append($": {(quantity.Float() / total.Float() * 100):0.####}%");
        }

        _rmcChat.ChatMessageToOne(msg.ToString(), user);
    }
}
