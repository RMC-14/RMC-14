using System.Linq;
using Content.Shared._RMC14.Requisitions;
using Content.Shared._RMC14.Requisitions.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Robust.Shared.Containers;

namespace Content.Server._RMC14.Requisitions;

public sealed partial class RequisitionsSystem
{
    [Dependency] private readonly SharedContainerSystem _formContainers = default!;
    [Dependency] private readonly SharedHandsSystem _formHands = default!;

    private const string FormsContainer = "requisition-forms";

    private void InitializeForms()
    {
        SubscribeLocalEvent<RequisitionsComputerComponent, EntInsertedIntoContainerMessage>(OnFormInserted);
        SubscribeLocalEvent<RequisitionsComputerComponent, EntRemovedFromContainerMessage>(OnFormRemoved);
        SubscribeLocalEvent<RequisitionsRequestPaperComponent, EntityTerminatingEvent>(OnFormTerminating);
        Subs.BuiEvents<RequisitionsComputerComponent>(RequisitionsUIKey.Key, subs =>
            subs.Event<RequisitionsEjectRequestMsg>(OnEjectForm));
    }

    private void OnInsertForm(Entity<RequisitionsComputerComponent> computer, ref InteractUsingEvent args)
    {
        if (args.Handled || !CanManageRequisitions(computer, args.User) ||
            !TryComp(args.Used, out RequisitionsRequestPaperComponent? form) ||
            computer.Comp.Account != form.Account ||
            !TryComp(form.Account, out RequisitionsAccountComponent? account) ||
            !account.Requests.Exists(request => request.Number == form.Number && !request.Closed))
            return;

        var container = _formContainers.EnsureContainer<Container>(computer, FormsContainer);
        if (container.ContainedEntities.Count >= account.MaxRequests ||
            computer.Comp.InsertedRequests.Contains(form.Number) ||
            !_formHands.TryDrop(args.User, args.Used, doDropInteraction: false) ||
            !_formContainers.Insert(args.Used, container))
            return;

        args.Handled = true;
    }

    private void OnFormInserted(
        Entity<RequisitionsComputerComponent> computer,
        ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != FormsContainer ||
            !TryComp(args.Entity, out RequisitionsRequestPaperComponent? form) ||
            computer.Comp.Account != form.Account ||
            !TryComp(form.Account, out RequisitionsAccountComponent? account) ||
            !account.Requests.Exists(request => request.Number == form.Number && !request.Closed) ||
            computer.Comp.InsertedRequests.Contains(form.Number))
            return;

        computer.Comp.InsertedRequests.Add(form.Number);
        SendUIStateAll();
    }

    private void OnFormRemoved(Entity<RequisitionsComputerComponent> computer, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != FormsContainer ||
            !TryComp(args.Entity, out RequisitionsRequestPaperComponent? form) ||
            computer.Comp.Account != form.Account ||
            !computer.Comp.InsertedRequests.Remove(form.Number))
            return;

        if (!TerminatingOrDeleted(computer))
            SendUIStateAll();
    }

    private void OnFormTerminating(Entity<RequisitionsRequestPaperComponent> form, ref EntityTerminatingEvent args)
    {
        if (!TryComp(form.Comp.Account, out RequisitionsAccountComponent? account) ||
            account.Requests.Find(request => request.Number == form.Comp.Number) is not { Closed: false } request)
            return;

        request.Closed = true;
        request.Revision++;
        if (request.Items.All(item => item.InTransit == 0))
            account.Requests.Remove(request);
        SendUIStateAll();
    }

    private void OnEjectForm(Entity<RequisitionsComputerComponent> computer, ref RequisitionsEjectRequestMsg args)
    {
        EjectForm(computer, args.Actor, args.Number);
    }

    private void EjectForm(Entity<RequisitionsComputerComponent> computer, EntityUid actor, int number)
    {
        if (!CanManageRequisitions(computer, actor) ||
            !_formContainers.TryGetContainer(computer, FormsContainer, out var container))
            return;

        foreach (var paper in container.ContainedEntities)
        {
            if (!TryComp(paper, out RequisitionsRequestPaperComponent? form) || form.Number != number)
                continue;

            if (!_formContainers.Remove(paper, container))
                return;
            _transform.SetCoordinates(paper, Transform(computer).Coordinates);
            _formHands.PickupOrDrop(actor, paper);
            return;
        }
    }
}
