using System.Linq;
using Content.Shared.Construction.Components;
using Content.Shared._RMC14.Crafting.Components;
using Content.Shared.Crafting.Events;
using Content.Shared.Crafting.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared.Crafting;
public sealed class SharedCraftingSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private List<LightCraftingPrototype> _lightPrototypes = default!;
    private List<string> _tags = new();
    private ISawmill _sawmill = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CraftableComponent, InteractUsingEvent>(OnInteract);
        SubscribeLocalEvent<CraftableComponent, LightCraftDoAfterEvent>(HandleLightDoAfter);

        SubscribeAllEvent<CraftStartedEvent>(OnCraftStarted);
        SubscribeAllEvent<DisassembleStartedEvent>(OnDisassembleStarted);
        SubscribeLocalEvent<StorageComponent, CraftDoAfterEvent>(HandleDoAfter);
        SubscribeLocalEvent<StorageComponent, DisassembleDoAfterEvent>(HandleDoAfterDisassemble);

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);

        RenewPrototypesCache();
        _sawmill = Logger.GetSawmill("crafts");
    }

    private void RenewPrototypesCache()
    {
        _lightPrototypes = _proto.EnumeratePrototypes<LightCraftingPrototype>().ToList();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if(args.WasModified<LightCraftingPrototype>())
        {
            RenewPrototypesCache();
        }
    }

    private bool IsEqualOrHasParent(string targetId, EntProtoId ingredient, bool applyExactMatch)
    {
        if (targetId == ingredient.Id)
        {
            return true;
        }
        if (applyExactMatch)
        {
            return false;
        }
        if (!_proto.TryIndex(targetId, out EntityPrototype? target) || target.Parents == null)
        {
            return false;
        }
        foreach (var parentId in target.Parents)
        {
            return IsEqualOrHasParent(parentId, ingredient, applyExactMatch);
        }

        return false;
    }

    private bool CheckStepForTargetOrUsed(StepDetails step, EntityUid target, EntityUid used)
    {
        var targetId = GetItemProtoID(target);
        var usedId = GetItemProtoID(used);
        if ((IsEqualOrHasParent(targetId, step.FirstIngredient, step.ExactFirst) && IsEqualOrHasParent(usedId, step.SecondIngredient, step.ExactSecond)) ||
            (IsEqualOrHasParent(usedId, step.FirstIngredient, step.ExactFirst) && IsEqualOrHasParent(targetId, step.SecondIngredient, step.ExactSecond)))
        {
            return true;
        }

        return false;
    }

    private bool CheckStepForTag(StepDetails step, EntityUid target, EntityUid used)
    {
        var targetId = GetItemProtoID(target);
        var usedId = GetItemProtoID(used);

        if ((IsEqualOrHasParent(targetId, step.FirstIngredient, step.ExactFirst) && _tags.Contains(step.SecondIngredient) && _tag.HasTag(used, step.SecondIngredient.Id)) ||
            (IsEqualOrHasParent(usedId, step.FirstIngredient, step.ExactFirst) && _tags.Contains(step.SecondIngredient) && _tag.HasTag(target, step.SecondIngredient.Id)) ||
            (IsEqualOrHasParent(targetId, step.SecondIngredient, step.ExactSecond) && _tags.Contains(step.FirstIngredient) && _tag.HasTag(used, step.FirstIngredient.Id)) ||
            (IsEqualOrHasParent(usedId, step.SecondIngredient, step.ExactSecond) && _tags.Contains(step.FirstIngredient) && _tag.HasTag(target, step.FirstIngredient.Id)))
        {
            return true;
        }

        return false;
    }

    private void OnInteract(EntityUid uid, CraftableComponent component, InteractUsingEvent args)
    {
        if (_tags.Count == 0)
        {
            foreach (var tag in _proto.EnumeratePrototypes<TagPrototype>().ToList())
            {
                _tags.Add(tag.ID);
            }
        }

        if (!HasComp<CraftableComponent>(args.Used))
            return;

        foreach (var prototype in _lightPrototypes)
        {
            if (CheckStepForTargetOrUsed(prototype.Steps, args.Target, args.Used))
            {
                StartLightDoAfter(args.User, args.Target, args.Used, prototype, prototype.Steps);
                _sawmill.Debug("Started light without tags");
                return;
            }

            if (CheckStepForTag(prototype.Steps, args.Target, args.Used))
            {
                StartLightDoAfter(args.User, args.Target, args.Used, prototype, prototype.Steps);
                _sawmill.Debug($"Started light with tags");
                return;
            }
        }
    }

    private void StartLightDoAfter(EntityUid user, EntityUid target, EntityUid used, LightCraftingPrototype prototype, StepDetails step)
    {
        var time = step.Time;
        var args = new DoAfterArgs(EntityManager, user, time, new LightCraftDoAfterEvent(prototype, step), target, target, used);
        _doAfter.TryStartDoAfter(args);
    }

    private void OnDisassembleStarted(DisassembleStartedEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession?.AttachedEntity;

        if (player == null)
            return;

        var ent = GetEntity(msg.StorageEnt);

        if (!TryComp<ContainerManagerComponent>(ent, out var storage))
            return;
        if (!TryComp<MetaDataComponent>(ent, out var meta) || meta.EntityPrototype == null)
            return;

        var workbenchId = meta.EntityPrototype.ID;
        var protos = _proto.EnumeratePrototypes<CraftingPrototype>();

        foreach (var proto in protos)
        {
            if (!DisassembleAvailable(proto, storage.Containers.First().Value, workbenchId))
                continue;

            StartDoAfterDisassemble(player.Value, ent, storage.Containers.First().Value, proto, proto.CraftTime);
            return;
        }
    }

    private void OnCraftStarted(CraftStartedEvent msg, EntitySessionEventArgs args)
    {
        var player = args.SenderSession?.AttachedEntity;

        if (player == null)
            return;

        var ent = GetEntity(msg.StorageEnt);

        if (!TryComp<ContainerManagerComponent>(ent, out var storage))
            return;

        if (!TryComp<MetaDataComponent>(ent, out var meta) || meta.EntityPrototype == null)
            return;

        var workbenchId = meta.EntityPrototype.ID;
        var protos = _proto.EnumeratePrototypes<CraftingPrototype>();

        foreach (var proto in protos)
        {
            var container = storage.Containers.First().Value;
            if (!CraftAvailable(proto, container, workbenchId))
                continue;

            StartDoAfter(player.Value, ent, container, proto, proto.CraftTime);
            return;
        }
    }

    private void StartDoAfterDisassemble(EntityUid player, EntityUid storageent, BaseContainer container, CraftingPrototype proto, float time)
    {
        var args = new DoAfterArgs(
            EntityManager,
            player,
            time,
            new DisassembleDoAfterEvent(proto),
            storageent,
            storageent)
        {
            BreakOnDamage = true,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(args);
    }

    private void StartDoAfter(EntityUid player, EntityUid storageent, BaseContainer container, CraftingPrototype proto, float time)
    {
        var args = new DoAfterArgs(
            EntityManager,
            player,
            time,
            new CraftDoAfterEvent(proto),
            storageent,
            storageent)
        {
            BreakOnDamage = true,
            BlockDuplicate = true
        };

        _doAfter.TryStartDoAfter(args);
    }

    private Dictionary<string, int> GetElementsInStorage(BaseContainer container)
    {
        var items = new Dictionary<string, int>();

        foreach (var item in container.ContainedEntities)
        {
            var itemId = GetItemProtoID(item);
            int amount = 1;
            if (TryComp<Stacks.StackComponent>(item, out var stack))
            {
                amount = stack.Count;
            }

            if (!items.TryAdd(itemId, amount))
            {
                items[itemId] += amount;
            }
        }

        return items;
    }

    public string GetItemProtoID(EntityUid item)
    {
        if (TryComp<MetaDataComponent>(item, out var meta) && meta.EntityPrototype != null)
        {
            return meta.EntityPrototype.ID;
        }

        return string.Empty;
    }
    private bool DictEquals(Dictionary<string, CraftingRecipeDetails> dict1, Dictionary<string, int> dict2, List<EntityUid> dict2Ents, CraftingPrototype proto)
    {
        // Create a copy of dict2 to track the counts
        var dict2Counts = new Dictionary<string, int>(dict2);

        foreach (var kv in dict1)
        {
            // If the current item is one of the ResultProtos, ignore it
            if (proto.ResultProtos.Contains(kv.Key))
            {
                _sawmill.Debug($"Skipped {kv.Key} in dict equality because it's a ResultProto");
                continue;
            }

            if (kv.Value.Tag)
            {
                // Check if there is an item with the same tag in dict2
                if (!dict2Ents.Any(itemId => _tag.HasTag(itemId, kv.Key)))
                {
                    return false; // Tag not found in dict2
                }
            }
            else
            {
                // If the current item is identified by an ID
                if (!(dict2Counts.TryGetValue(kv.Key, out var count) && count >= kv.Value.Amount))
                {
                    return false; // ID not found or count not sufficient in dict2
                }
            }
        }

        // If all items in dict1 have matching items in dict2, excluding ResultProtos
        return dict1.Keys.Count == dict2.Keys.Except(proto.ResultProtos).Count();
    }

    private bool CraftAvailable(CraftingPrototype proto, BaseContainer container, string? workbenchId)
    {
        var items = GetElementsInStorage(container);
        if (!DictEquals(proto.Items, items, container.ContainedEntities.ToList(), proto))
            return false;

        // Check if the recipe requires a specific workbench
        if (!string.IsNullOrEmpty(proto.RequiredWorkbench) && workbenchId != null)
        {
            if (workbenchId != proto.RequiredWorkbench)
                return false;
        }

        return true;
    }

    private bool DisassembleAvailable(CraftingPrototype proto, BaseContainer container, string? workbenchId)
    {
        var items = GetElementsInStorage(container);
        var item = items.FirstOrDefault();
        bool isSingle = items.Count == 1 && proto.ResultProtos.Count == 1 && item.Value == 1;

        if (!isSingle || item.Key != proto.ResultProtos.FirstOrDefault())
            return false;

        // Check if the recipe requires a specific workbench for disassembly
        if (!string.IsNullOrEmpty(proto.RequiredWorkbench))
        {
            if (workbenchId != proto.RequiredWorkbench)
                return false;
        }

        return true;
    }

    private void RemoveIfNeeded(EntityUid entity, string entityId, EntProtoId ingredient, bool keep, bool exact)
    {
        if (!keep && IsEqualOrHasParent(entityId, ingredient, exact))
        {
            QueueDel(entity);
            _sawmill.Debug($"Removed {ingredient}");
        }
    }

    private void LightCraft(EntityUid target, EntityUid used, LightCraftingPrototype prototype, StepDetails step)
    {
        if (_net.IsClient)
            return;

        var targetId = GetItemProtoID(target);
        var usedId = GetItemProtoID(used);
        var xform = _transform.GetMapCoordinates(target);

        RemoveIfNeeded(target, targetId, step.FirstIngredient, step.KeepFirst, step.ExactFirst);
        RemoveIfNeeded(target, targetId, step.SecondIngredient, step.KeepSecond, step.ExactSecond);
        RemoveIfNeeded(used, usedId, step.FirstIngredient, step.KeepFirst, step.ExactFirst);
        RemoveIfNeeded(used, usedId, step.SecondIngredient, step.KeepSecond, step.ExactSecond);

        foreach (var item in prototype.Results)
        {
            var newEntity = Spawn(item, xform);
            if (TryComp(newEntity, out TransformComponent? newTransfromComp) && TryComp(target, out TransformComponent? prevTransformComp))
            {
                if (newTransfromComp != null && prevTransformComp != null)
                {
                    newTransfromComp.LocalRotation = prevTransformComp.LocalRotation;
                }
            }
            var id = newEntity.Id;
            _sawmill.Debug($"Id: {id}");
        }
    }

    private void Disassemble(EntityUid user, EntityUid workbench, BaseContainer container, CraftingPrototype proto)
    {
        if (_net.IsClient)
            return;

        if (!TryComp<MetaDataComponent>(workbench, out var meta) || meta.EntityPrototype == null)
            return;

        var workbenchId = meta.EntityPrototype.ID;
        if (!DisassembleAvailable(proto, container, workbenchId))
            return;

        // Find the result item to disassemble
        var itemToDisassemble = container.ContainedEntities.FirstOrDefault(e => GetItemProtoID(e) == proto.ResultProtos.FirstOrDefault());

        if (itemToDisassemble == default)
            return;

        // Remove the item to be disassembled
        _container.Remove(itemToDisassemble, container);
        _sawmill.Debug($"Removed {Name(itemToDisassemble)} with id: {itemToDisassemble.Id} for disassembly");
        QueueDel(itemToDisassemble);

        var toInsertList = new List<EntityUid>();

        // Spawn the component items based on chance
        foreach (var kvp in proto.Items)
        {
            var itemId = kvp.Key;

            bool isBlueprint = _proto.Index(itemId).HasComponent<CraftingBlueprintComponent>();

            var itemDetails = kvp.Value;

            if (itemDetails.Catalyzer && !isBlueprint)
                continue;

            for (int i = 0; i < itemDetails.Amount; i++)
            {
                if (_random.NextFloat() <= proto.DisassembleChance)
                {
                    var spawnPosition = Transform(user).Coordinates;
                    var toInsert = Spawn(itemId, spawnPosition);
                    _sawmill.Debug($"Spawned {itemId} from disassembly");
                    toInsertList.Add(toInsert);
                }
                else
                {
                    _sawmill.Debug($"Did not spawn {itemId} due to disassemble chance");
                }
            }
        }

        // Insert all spawned items into the container
        foreach (var toInsert in toInsertList)
        {
            if (_container.CanInsert(toInsert, container))
            {
                _container.Insert(toInsert, container);
                _sawmill.Debug($"Inserted {Name(toInsert)} with id: {toInsert} from disassembly");
            }
            else
            {
                // If can't insert, drop it at the user's position
                var userPosition = Transform(user).Coordinates;
                _transform.SetCoordinates(toInsert, userPosition);
                _sawmill.Debug($"Dropped {Name(toInsert)} with id: {toInsert} at user's position");
            }
        }
    }

    private void Craft(EntityUid user, EntityUid workbench, BaseContainer container, CraftingPrototype proto)
    {
        if (_net.IsClient)
            return;
        if (!TryComp<MetaDataComponent>(workbench, out var meta) || meta.EntityPrototype == null)
            return;

        var workbenchId = meta.EntityPrototype.ID;
        while (CraftAvailable(proto, container, workbenchId))
        {
            // Remove required ingredients
            foreach (var (itemId, requiredAmount) in proto.Items)
            {
                if (requiredAmount.Catalyzer)
                    continue;

                RemoveRequiredAmount(container, itemId, requiredAmount.Amount, requiredAmount.Tag);
            }

            // Spawn and insert result items
            foreach (var item in proto.ResultProtos)
            {
                var spawnPosition = Transform(user).Coordinates;
                var toInsert = Spawn(item, spawnPosition);
                if (_container.CanInsert(toInsert, container))
                {
                    _container.Insert(toInsert, container);
                    _sawmill.Debug($"Inserted {Name(toInsert)} with id: {toInsert}");
                }
            }
        }
    }


    private void RemoveRequiredAmount(BaseContainer container, string itemId, int requiredAmount, bool tag)
    {
        var remainingToRemove = requiredAmount;
        var entitiesToRemove = new List<(EntityUid Entity, int AmountToRemove)>();

        foreach (var entity in container.ContainedEntities)
        {
            var shouldSkip = tag
                ? !_tag.HasTag(entity, itemId)
                : GetItemProtoID(entity) != itemId;

            if (shouldSkip)
                continue;

            if (TryComp<Stacks.StackComponent>(entity, out var stack))
            {
                var amountToRemove = Math.Min(stack.Count, remainingToRemove);
                entitiesToRemove.Add((entity, amountToRemove));
                remainingToRemove -= amountToRemove;
            }
            else
            {
                entitiesToRemove.Add((entity, 1));
                remainingToRemove--;
            }

            if (remainingToRemove <= 0)
                break;
        }

        foreach (var (entity, amountToRemove) in entitiesToRemove)
        {
            if (TryComp<Stacks.StackComponent>(entity, out var stack))
            {
                if (stack.Count <= amountToRemove)
                {
                    _container.Remove(entity, container);
                    QueueDel(entity);
                }
                else
                {
                    ReduceStackCount(entity, amountToRemove);
                }
            }
            else
            {
                _container.Remove(entity, container);
                QueueDel(entity);
            }
        }
    }
    private void ReduceStackCount(EntityUid entity, int amount)
    {
        if (TryComp<Stacks.StackComponent>(entity, out var stack))
        {
            stack.Count -= amount;
            Dirty(entity, stack);
        }
    }

    private void DropItemAtUserPosition(EntityUid user, EntityUid item)
    {
    var userPosition = Transform(user).Coordinates;
    _transform.SetCoordinates(item, userPosition);
    // You might want to add additional logic here to make the item visible and interactable
}
    private void HandleDoAfter(EntityUid uid, StorageComponent component, CraftDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (!args.Target.HasValue)
            return;
        Craft(args.User, uid, component.Container, args.Proto);
    }

    private void HandleDoAfterDisassemble(EntityUid uid, StorageComponent component, DisassembleDoAfterEvent args)
    {
        if (args.Cancelled)
            return;
        if (!args.Target.HasValue)
            return;
        Disassemble(args.User, uid, component.Container, args.Proto);
    }

    private void HandleLightDoAfter(EntityUid uid, CraftableComponent component, LightCraftDoAfterEvent args)
    {
        if (args.Cancelled || args.Target == null || args.Used == null)
            return;

        LightCraft(args.Target.Value, args.Used.Value, args.Proto, args.Step);
    }
}
[Serializable, NetSerializable]
public sealed partial class CraftDoAfterEvent : DoAfterEvent
{
    [DataField]
    public CraftingPrototype Proto = default!;

    public CraftDoAfterEvent(CraftingPrototype proto)
    {
        Proto = proto;
    }

    private CraftDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class DisassembleDoAfterEvent : DoAfterEvent
{
    [DataField]
    public CraftingPrototype Proto = default!;

    public DisassembleDoAfterEvent(CraftingPrototype proto)
    {
        Proto = proto;
    }

    private DisassembleDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable]
public sealed partial class LightCraftDoAfterEvent : DoAfterEvent
{
    [DataField]
    public LightCraftingPrototype Proto = default!;

    [DataField]
    public StepDetails Step = default!;

    public LightCraftDoAfterEvent(LightCraftingPrototype proto, StepDetails step)
    {
        Proto = proto;
        Step = step;
    }

    private LightCraftDoAfterEvent()
    {
    }
    public override DoAfterEvent Clone() => this;
}
