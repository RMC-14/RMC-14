using Content.Server.DeviceLinking.Systems;
using Content.Server.Hands.Systems;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Construction.EntitySystems;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Power;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Content.Shared.Stacks;
using Robust.Shared.Utility;
using Content.Server._RMC14.Kitchen.Components;
using Content.Shared._RMC14.Kitchen.Components;
using Content.Shared._RMC14.Kitchen;
using Content.Shared.Kitchen;

namespace Content.Server._RMC14.Kitchen.EntitySystems
{
    public sealed class ProcessorSystem : EntitySystem
    {
        [Dependency] private readonly DeviceLinkSystem _deviceLink = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly PowerReceiverSystem _power = default!;
        [Dependency] private readonly ProcessorRecipeManager _processorRecipeManager = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterface = default!;
        [Dependency] private readonly HandsSystem _handsSystem = default!;
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly SharedStackSystem _stack = default!;
        [Dependency] private readonly IPrototypeManager _prototype = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ProcessorComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ProcessorComponent, MapInitEvent>(OnMapInit);
            SubscribeLocalEvent<ProcessorComponent, SolutionContainerChangedEvent>(OnSolutionChange);
            SubscribeLocalEvent<ProcessorComponent, EntInsertedIntoContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<ProcessorComponent, EntRemovedFromContainerMessage>(OnContentUpdate);
            SubscribeLocalEvent<ProcessorComponent, InteractUsingEvent>(OnInteractUsing, after: new[] { typeof(AnchorableSystem) });
            SubscribeLocalEvent<ProcessorComponent, ContainerIsInsertingAttemptEvent>(OnInsertAttempt);
            SubscribeLocalEvent<ProcessorComponent, AnchorStateChangedEvent>(OnAnchorChanged);

            SubscribeLocalEvent<ProcessorComponent, SignalReceivedEvent>(OnSignalReceived);

            SubscribeLocalEvent<ProcessorComponent, ProcessorStartCookMessage>((u, c, m) => Wzhzhzh(u, c, m.Actor));
            SubscribeLocalEvent<ProcessorComponent, ProcessorEjectMessage>(OnEjectMessage);
            SubscribeLocalEvent<ProcessorComponent, ProcessorEjectSolidIndexedMessage>(OnEjectIndex);

            SubscribeLocalEvent<ActiveProcessorComponent, ComponentStartup>(OnProcessorStart);
            SubscribeLocalEvent<ActiveProcessorComponent, ComponentShutdown>(OnProcessorStop);
            SubscribeLocalEvent<ActiveProcessorComponent, EntInsertedIntoContainerMessage>(OnActiveProcessorInsert);
            SubscribeLocalEvent<ActiveProcessorComponent, EntRemovedFromContainerMessage>(OnActiveProcessorRemove);

            SubscribeLocalEvent<ProcessorRecipeProviderComponent, GetSecretProcessorRecipesEvent>(OnGetProcessorSecretRecipes);
        }

        private void OnProcessorStart(Entity<ActiveProcessorComponent> ent, ref ComponentStartup args)
        {
            if (!TryComp<ProcessorComponent>(ent, out var processorComponent))
                return;
            SetAppearance(ent.Owner, ProcessorVisualState.Processing, processorComponent);

            processorComponent.PlayingStream =
                _audio.PlayPvs(processorComponent.LoopingSound, ent, AudioParams.Default.WithLoop(true).WithMaxDistance(5))?.Entity;
        }

        private void OnProcessorStop(Entity<ActiveProcessorComponent> ent, ref ComponentShutdown args)
        {
            if (!TryComp<ProcessorComponent>(ent, out var processorComponent))
                return;

            SetAppearance(ent.Owner, ProcessorVisualState.On, processorComponent);
            processorComponent.PlayingStream = _audio.Stop(processorComponent.PlayingStream);
        }

        private void OnActiveProcessorInsert(Entity<ActiveProcessorComponent> ent, ref EntInsertedIntoContainerMessage args)
        {
            var processorComp = AddComp<ActivelyProcessingComponent>(args.Entity);
            processorComp.Processor = ent.Owner;
        }

        private void OnActiveProcessorRemove(Entity<ActiveProcessorComponent> ent, ref EntRemovedFromContainerMessage args)
        {
            EntityManager.RemoveComponentDeferred<ActivelyProcessingComponent>(args.Entity);
        }

        private void SubtractContents(ProcessorComponent component, ProcessorRecipePrototype recipe)
        {
            // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]

            var totalReagentsToRemove = new Dictionary<string, FixedPoint2>(recipe.IngredientsReagents);

            // this is spaghetti ngl
            foreach (var item in component.Storage.ContainedEntities)
            {
                // use the same reagents as when we selected the recipe
                if (!_solutionContainer.TryGetDrainableSolution(item, out var solutionEntity, out var solution))
                    continue;

                foreach (var (reagent, _) in recipe.IngredientsReagents)
                {
                    // removed everything
                    if (!totalReagentsToRemove.ContainsKey(reagent))
                        continue;

                    var quant = solution.GetTotalPrototypeQuantity(reagent);

                    if (quant >= totalReagentsToRemove[reagent])
                    {
                        quant = totalReagentsToRemove[reagent];
                        totalReagentsToRemove.Remove(reagent);
                    }
                    else
                    {
                        totalReagentsToRemove[reagent] -= quant;
                    }

                    _solutionContainer.RemoveReagent(solutionEntity.Value, reagent, quant);
                }
            }

            foreach (var recipeSolid in recipe.IngredientsSolids)
            {
                for (var i = 0; i < recipeSolid.Value; i++)
                {
                    foreach (var item in component.Storage.ContainedEntities)
                    {
                        string? itemID = null;

                        // If an entity has a stack component, use the stacktype instead of prototype id
                        if (TryComp<StackComponent>(item, out var stackComp))
                        {
                            itemID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                        }
                        else
                        {
                            var metaData = MetaData(item);
                            if (metaData.EntityPrototype == null)
                            {
                                continue;
                            }
                            itemID = metaData.EntityPrototype.ID;
                        }

                        if (itemID != recipeSolid.Key)
                        {
                            continue;
                        }

                        if (stackComp is not null)
                        {
                            if (stackComp.Count == 1)
                            {
                                _container.Remove(item, component.Storage);
                            }
                            _stack.Use(item, 1, stackComp);
                            break;
                        }
                        else
                        {
                            _container.Remove(item, component.Storage);
                            Del(item);
                            break;
                        }
                    }
                }
            }
        }

        private void OnInit(Entity<ProcessorComponent> ent, ref ComponentInit args)
        {
            // this really does have to be in ComponentInit
            ent.Comp.Storage = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        }

        private void OnMapInit(Entity<ProcessorComponent> ent, ref MapInitEvent args)
        {
            _deviceLink.EnsureSinkPorts(ent, ent.Comp.OnPort);
        }

        private void OnSolutionChange(Entity<ProcessorComponent> ent, ref SolutionContainerChangedEvent args)
        {
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnContentUpdate(EntityUid uid, ProcessorComponent component, ContainerModifiedMessage args) // For some reason ContainerModifiedMessage just can't be used at all with Entity<T>. TODO: replace with Entity<T> syntax once that's possible
        {
            if (component.Storage != args.Container)
                return;

            UpdateUserInterfaceState(uid, component);
        }

        private void OnInsertAttempt(Entity<ProcessorComponent> ent, ref ContainerIsInsertingAttemptEvent args)
        {
            if (args.Container.ID != ent.Comp.ContainerId)
                return;

            if (ent.Comp.Broken)
            {
                args.Cancel();
                return;
            }

            if (TryComp<ItemComponent>(args.EntityUid, out var item))
            {
                if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
                {
                    args.Cancel();
                    return;
                }
            }
            else
            {
                args.Cancel();
                return;
            }

            if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
                args.Cancel();
        }

        private void OnInteractUsing(Entity<ProcessorComponent> ent, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;
            if (!(TryComp<ApcPowerReceiverComponent>(ent, out var apc) && apc.Powered))
            {
                _popupSystem.PopupEntity(Loc.GetString("processor-component-interact-using-no-power"), ent, args.User);
                return;
            }

            if (ent.Comp.Broken)
            {
                _popupSystem.PopupEntity(Loc.GetString("processor-component-interact-using-broken"), ent, args.User);
                return;
            }

            if (TryComp<ItemComponent>(args.Used, out var item))
            {
                // check if size of an item you're trying to put in is too big
                if (_item.GetSizePrototype(item.Size) > _item.GetSizePrototype(ent.Comp.MaxItemSize))
                {
                    _popupSystem.PopupEntity(Loc.GetString("processor-component-interact-item-too-big", ("item", args.Used)), ent, args.User);
                    return;
                }
            }
            else
            {
                // check if thing you're trying to put in isn't an item
                _popupSystem.PopupEntity(Loc.GetString("processor-component-interact-using-transfer-fail"), ent, args.User);
                return;
            }

            if (ent.Comp.Storage.Count >= ent.Comp.Capacity)
            {
                _popupSystem.PopupEntity(Loc.GetString("processor-component-interact-full"), ent, args.User);
                return;
            }

            args.Handled = true;
            _handsSystem.TryDropIntoContainer(args.User, args.Used, ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnAnchorChanged(EntityUid uid, ProcessorComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
                _container.EmptyContainer(component.Storage);
        }

        private void OnSignalReceived(Entity<ProcessorComponent> ent, ref SignalReceivedEvent args)
        {
            if (args.Port != ent.Comp.OnPort)
                return;

            if (ent.Comp.Broken || !_power.IsPowered(ent))
                return;

            Wzhzhzh(ent.Owner, ent.Comp, null);
        }

        public void UpdateUserInterfaceState(EntityUid uid, ProcessorComponent component)
        {
            _userInterface.SetUiState(uid, ProcessorUiKey.Key, new ProcessorUpdateUserInterfaceState(
                GetNetEntityArray(component.Storage.ContainedEntities.ToArray()),
                HasComp<ActiveProcessorComponent>(uid),
                component.CurrentCookTimeButtonIndex,
                component.CurrentCookTimerTime,
                component.CurrentCookTimeEnd
            ));
        }

        public void SetAppearance(EntityUid uid, ProcessorVisualState state, ProcessorComponent? component = null, AppearanceComponent? appearanceComponent = null)
        {
            if (!Resolve(uid, ref component, ref appearanceComponent, false))
                return;
            var display = state;
            _appearance.SetData(uid, PowerDeviceVisuals.VisualState, display, appearanceComponent);
        }

        public static bool HasContents(ProcessorComponent component)
        {
            return component.Storage.ContainedEntities.Any();
        }

        /// <summary>
        /// Starts Cooking
        /// </summary>
        /// <remarks>
        /// It does not make a "wzhzhzh" sound, it makes a "mmmmmmmm" sound!
        /// -emo
        /// </remarks>
        public void Wzhzhzh(EntityUid uid, ProcessorComponent component, EntityUid? user)
        {
            if (!HasContents(component) || HasComp<ActiveProcessorComponent>(uid) || !(TryComp<ApcPowerReceiverComponent>(uid, out var apc) && apc.Powered))
                return;

            var solidsDict = new Dictionary<string, int>();
            var reagentDict = new Dictionary<string, FixedPoint2>();
            // TODO use lists of Reagent quantities instead of reagent prototype ids.
            foreach (var item in component.Storage.ContainedEntities.ToArray())
            {
                // special behavior when being processor ;)
                var ev = new BeingProcessorEvent(uid, user);
                RaiseLocalEvent(item, ev);

                if (ev.Handled)
                {
                    UpdateUserInterfaceState(uid, component);
                    return;
                }

                var processorComp = AddComp<ActivelyProcessingComponent>(item);
                processorComp.Processor = uid;

                string? solidID = null;
                var amountToAdd = 1;

                // If a processor recipe uses a stacked item, use the default stack prototype id instead of prototype id
                if (TryComp<StackComponent>(item, out var stackComp))
                {
                    solidID = _prototype.Index<StackPrototype>(stackComp.StackTypeId).Spawn;
                    amountToAdd = stackComp.Count;
                }
                else
                {
                    var metaData = MetaData(item); //this simply begs for cooking refactor
                    if (metaData.EntityPrototype is not null)
                        solidID = metaData.EntityPrototype.ID;
                }

                if (solidID is null)
                    continue;

                if (!solidsDict.TryAdd(solidID, amountToAdd))
                    solidsDict[solidID] += amountToAdd;

                // only use reagents we have access to
                // you have to break the eggs before we can use them!
                if (!_solutionContainer.TryGetDrainableSolution(item, out var _, out var solution))
                    continue;

                foreach (var (reagent, quantity) in solution.Contents)
                {
                    if (!reagentDict.TryAdd(reagent.Prototype, quantity))
                        reagentDict[reagent.Prototype] += quantity;
                }
            }

            // Check recipes
            var getRecipesEv = new GetSecretProcessorRecipesEvent();
            RaiseLocalEvent(uid, ref getRecipesEv);

            List<ProcessorRecipePrototype> recipes = getRecipesEv.Recipes;
            recipes.AddRange(_processorRecipeManager.Recipes);
            var portionedRecipe = recipes.Select(r =>
                CanSatisfyRecipe(component, r, solidsDict, reagentDict)).FirstOrDefault(r => r.Item2 > 0);

            var activeComp = AddComp<ActiveProcessorComponent>(uid); //processor is now cooking
            activeComp.CookTimeRemaining = component.CurrentCookTimerTime * component.CookTimeMultiplier;
            activeComp.TotalTime = component.CurrentCookTimerTime; //this doesn't scale so that we can have the "actual" time
            activeComp.PortionedRecipe = portionedRecipe;
            //Scale tiems with cook times
            component.CurrentCookTimeEnd = _gameTiming.CurTime + TimeSpan.FromSeconds(component.CurrentCookTimerTime * component.CookTimeMultiplier);
            UpdateUserInterfaceState(uid, component);
        }

        private void StopCooking(Entity<ProcessorComponent> ent)
        {
            RemCompDeferred<ActiveProcessorComponent>(ent);
            foreach (var solid in ent.Comp.Storage.ContainedEntities)
            {
                RemCompDeferred<ActivelyProcessingComponent>(solid);
            }
        }

        public static (ProcessorRecipePrototype, int) CanSatisfyRecipe(ProcessorComponent component, ProcessorRecipePrototype recipe, Dictionary<string, int> solids, Dictionary<string, FixedPoint2> reagents)
        {
            var portions = 0;

            if (component.CurrentCookTimerTime % recipe.CookTime != 0)
            {
                //can't be a multiple of this recipe
                return (recipe, 0);
            }

            foreach (var solid in recipe.IngredientsSolids)
            {
                if (!solids.ContainsKey(solid.Key))
                    return (recipe, 0);

                if (solids[solid.Key] < solid.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? solids[solid.Key] / solid.Value.Int()
                    : Math.Min(portions, solids[solid.Key] / solid.Value.Int());
            }

            foreach (var reagent in recipe.IngredientsReagents)
            {
                // TODO Turn recipe.IngredientsReagents into a ReagentQuantity[]
                if (!reagents.ContainsKey(reagent.Key))
                    return (recipe, 0);

                if (reagents[reagent.Key] < reagent.Value)
                    return (recipe, 0);

                portions = portions == 0
                    ? reagents[reagent.Key].Int() / reagent.Value.Int()
                    : Math.Min(portions, reagents[reagent.Key].Int() / reagent.Value.Int());
            }

            //cook only as many of those portions as time allows
            return (recipe, (int)Math.Min(portions, component.CurrentCookTimerTime / recipe.CookTime));
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            var query = EntityQueryEnumerator<ActiveProcessorComponent, ProcessorComponent>();
            while (query.MoveNext(out var uid, out var active, out var processor))
            {

                active.CookTimeRemaining -= frameTime;

                //check if there's still cook time left
                if (active.CookTimeRemaining > 0)
                {
                    continue;
                }

                //this means the processor has finished cooking.

                if (active.PortionedRecipe.Item1 != null)
                {
                    var coords = Transform(uid).Coordinates;
                    for (var i = 0; i < active.PortionedRecipe.Item2; i++)
                    {
                        SubtractContents(processor, active.PortionedRecipe.Item1);
                        Spawn(active.PortionedRecipe.Item1.Result, coords);
                    }
                }

                _container.EmptyContainer(processor.Storage);
                processor.CurrentCookTimeEnd = TimeSpan.Zero;
                UpdateUserInterfaceState(uid, processor);
                StopCooking((uid, processor));
            }
        }

        /// <summary>
        /// This event tries to get secret recipes that the processor might be capable of.
        /// Currently, we only check the processor itself, but in the future, the user might be able to learn recipes.
        /// </summary>
        private void OnGetProcessorSecretRecipes(Entity<ProcessorRecipeProviderComponent> ent, ref GetSecretProcessorRecipesEvent args)
        {
            foreach (var recipeId in ent.Comp.ProvidedRecipes)
            {
                if (_prototype.TryIndex(recipeId, out var recipeProto))
                {
                    args.Recipes.Add(recipeProto);
                }
            }
        }

        #region ui
        private void OnEjectMessage(Entity<ProcessorComponent> ent, ref ProcessorEjectMessage args)
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveProcessorComponent>(ent))
                return;

            _container.EmptyContainer(ent.Comp.Storage);
            _audio.PlayPvs(ent.Comp.ClickSound, ent, AudioParams.Default.WithVolume(-2));
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        private void OnEjectIndex(Entity<ProcessorComponent> ent, ref ProcessorEjectSolidIndexedMessage args)
        {
            if (!HasContents(ent.Comp) || HasComp<ActiveProcessorComponent>(ent))
                return;

            _container.Remove(EntityManager.GetEntity(args.EntityID), ent.Comp.Storage);
            UpdateUserInterfaceState(ent, ent.Comp);
        }

        #endregion
    }
}
