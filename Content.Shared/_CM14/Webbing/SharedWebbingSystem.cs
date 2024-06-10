using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Shared._CM14.Webbing;

public abstract class SharedWebbingSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedItemSystem _item = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedStorageSystem _storage = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<WebbingClothingComponent, InteractUsingEvent>(OnWebbingClothingInteractUsing);
        SubscribeLocalEvent<WebbingClothingComponent, GetVerbsEvent<InteractionVerb>>(OnWebbingClothingGetVerbs);
        SubscribeLocalEvent<WebbingClothingComponent, ActivateInWorldEvent>(OnWebbingClothingActivateInWorld);
        SubscribeLocalEvent<WebbingClothingComponent, EntInsertedIntoContainerMessage>(OnClothingInserted);
        SubscribeLocalEvent<WebbingClothingComponent, EntRemovedFromContainerMessage>(OnClothingRemoved);
    }

    private void OnWebbingClothingInteractUsing(Entity<WebbingClothingComponent> clothing, ref InteractUsingEvent args)
    {
        if (Attach(clothing, args.Used))
            args.Handled = true;
    }

    private void OnWebbingClothingGetVerbs(Entity<WebbingClothingComponent> clothing, ref GetVerbsEvent<InteractionVerb> args)
    {
        if (!HasWebbing((clothing, clothing), out _))
            return;

        var user = args.User;
        args.Verbs.Add(new InteractionVerb
        {
            Text = "Remove webbing",
            Act = () => RemoveWebbing(clothing, user)
        });
    }

    private void OnWebbingClothingActivateInWorld(Entity<WebbingClothingComponent> ent, ref ActivateInWorldEvent args)
    {
        OpenStorage(ent, args.User);
    }

    public void OpenStorage(Entity<WebbingClothingComponent> clothing, EntityUid user)
    {
        if (HasWebbing((clothing, clothing), out var webbing))
            _storage.OpenStorageUI(webbing, user);
    }

    public bool HasWebbing(Entity<WebbingClothingComponent?> clothing, out Entity<WebbingComponent> webbing)
    {
        webbing = default;
        if (!Resolve(clothing, ref clothing.Comp, false))
            return false;

        if (!_container.TryGetContainer(clothing, clothing.Comp.Container, out var container) ||
            container.Count <= 0)
        {
            return false;
        }

        var ent = container.ContainedEntities[0];
        if (!TryComp(ent, out WebbingComponent? webbingComp))
        {
            return false;
        }

        webbing = (ent, webbingComp);
        return true;
    }

    public bool Fits(EntityUid? clothing, EntityUid held)
    {
        if (!TryComp(clothing, out WebbingClothingComponent? clothingComp))
            return false;

        // Attach webbing
        if (clothingComp.Webbing == null &&
            HasComp<WebbingComponent>(held))
        {
            return true;
        }

        // Insert into webbing
        if (clothingComp.Webbing is { } webbing &&
            _storage.CanInsert(webbing, held, out _))
        {
            return true;
        }

        return false;
    }

    private void RemoveWebbing(Entity<WebbingClothingComponent> clothing, EntityUid user)
    {
        if (TerminatingOrDeleted(clothing) || !clothing.Comp.Running)
            return;

        if (!HasWebbing((clothing, clothing), out var webbing))
            return;

        _container.TryRemoveFromContainer(webbing);
        _hands.TryPickupAnyHand(user, webbing);
    }

    protected virtual void OnClothingInserted(Entity<WebbingClothingComponent> clothing, ref EntInsertedIntoContainerMessage args)
    {
        if (clothing.Comp.Container == args.Container.ID)
        {
            clothing.Comp.Webbing = args.Entity;
            Dirty(clothing);
            _item.VisualsChanged(clothing);
        }
    }

    protected virtual void OnClothingRemoved(Entity<WebbingClothingComponent> clothing, ref EntRemovedFromContainerMessage args)
    {
        if (clothing.Comp.Container == args.Container.ID)
        {
            clothing.Comp.Webbing = null;
            Dirty(clothing);
            _item.VisualsChanged(clothing);
        }
    }

    public bool Attach(Entity<WebbingClothingComponent> clothing, EntityUid webbing)
    {
        if (!HasComp<WebbingComponent>(webbing) ||
            HasComp<StorageComponent>(clothing))
        {
            return false;
        }

        var container = _container.EnsureContainer<ContainerSlot>(clothing, clothing.Comp.Container);
        if (container.Count > 0 || !_container.Insert(webbing, container))
            return false;

        return true;
    }
}
