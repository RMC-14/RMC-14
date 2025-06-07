using Content.Server.Labels;
using Content.Server.Nutrition.Components;
using Content.Server.Nutrition.EntitySystems;
using Content.Server.Popups;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.Food;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Labels.Components;
using Content.Shared.Mobs.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Storage;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Discord;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server._RMC14.Food;
public sealed class RMCFoodSystem : SharedRMCFoodSystem
{
    [Dependency] private readonly FoodSystem _food = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solu = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly LabelSystem _label = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCFoodScoopingComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
        SubscribeLocalEvent<RMCFoodScoopingComponent, AfterInteractEvent>(OnInteract, before: [typeof(FoodSystem), typeof(UtensilSystem)]);
        SubscribeLocalEvent<RMCFoodScoopingComponent, UseInHandEvent>(OnUseInHand, before: [typeof(FoodSystem)]);
        SubscribeLocalEvent<RMCFoodScoopingComponent, ConsumeDoAfterEvent>(AfterConsume, after: [typeof(FoodSystem)]);

        SubscribeLocalEvent<RMCLabelByContainedComponent, MapInitEvent>(OnMapInitContained, after: [typeof(StorageSystem)]);
    }

    private void OnMapInitContained(Entity<RMCLabelByContainedComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<StorageComponent>(ent, out var storage))
            return;

        LabelComponent? itemLabel = null;

        foreach (var thing in storage.StoredItems)
        {
            if (!_tag.HasTag(thing.Key, ent.Comp.TagToCheck))
                continue;

            if (!TryComp<LabelComponent>(thing.Key, out var label))
                continue;

            itemLabel = label;
        }

        if (itemLabel == null)
            return;

        _label.Label(ent, itemLabel.CurrentLabel);
    }

    private void OnUseInHand(Entity<RMCFoodScoopingComponent> entity, ref UseInHandEvent args)
    {
        //Handle so we don't eat it
        args.Handled = true;
    }

    private void OnUtilityVerb(Entity<RMCFoodScoopingComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || !HasComp<FoodComponent>(args.Target))
            return;

        var user = args.User;
        var target = args.Target;

        var verb = new UtilityVerb()
        {
            Act = () => TryScoopFood(entity, user, target),
            Icon = new SpriteSpecifier.Rsi(new ResPath("/Textures/_RMC14/Objects/Tools/Kitchen/Spoon.rsi"), "plastic"),
            Text = Loc.GetString(Loc.GetString("rmc-scoop-verb-text")),
        };

        args.Verbs.Add(verb);
    }

    private void TryScoopFood(Entity<RMCFoodScoopingComponent> utensil, EntityUid user, EntityUid target, bool message = true)
    {
        if (!TryComp<FoodComponent>(target, out var food))
            return;

        if (!_solu.TryGetSolution(utensil.Owner, utensil.Comp.SpoonSolution, out var solc))
            return;

        if (!_solu.TryGetSolution(target, food.Solution, out var solf))
            return;

        if (TryComp<UtensilComponent>(utensil, out var uten) && (uten.Types & food.Utensil) == 0)
            return;

        if (solc.Value.Comp.Solution.AvailableVolume == 0)
        {
            _popup.PopupEntity(Loc.GetString("rmc-scoop-food-full", ("tool", utensil)), user, user);
            return;
        }

        _solu.TryTransferSolution(solc.Value, solf.Value.Comp.Solution, solc.Value.Comp.Solution.AvailableVolume);
        utensil.Comp.FoodName = Name(target);

        var flavorScoop = EnsureComp<FlavorProfileComponent>(utensil);
        flavorScoop.Flavors.Clear();

        //Copy flavors
        if (TryComp<FlavorProfileComponent>(target, out var flavor))
            flavorScoop.Flavors.UnionWith(flavor.Flavors);

        utensil.Comp.LastFood = target;

        if (message)
        {
            _popup.PopupEntity(Loc.GetString("rmc-scoop-food-self", ("food", target), ("tool", utensil)), user, user);
            _popup.PopupEntity(Loc.GetString("rmc-scoop-food-others", ("food", target), ("tool", utensil), ("user", user)), user, Filter.PvsExcept(user), true);
        }

        if (_food.GetUsesRemaining(target, food) > 0)
            return;

        _food.DeleteAndSpawnTrash(food, target, user);
    }

    private void OnInteract(Entity<RMCFoodScoopingComponent> utensil, ref AfterInteractEvent args)
    {
        if (!TryComp<FoodComponent>(utensil, out var foodSpoon) || args.Target == null)
            return;

        if (HasComp<FoodComponent>(args.Target))
        {
            args.Handled = true;
            TryScoopFood(utensil, args.User, args.Target.Value, false);
            ScoopableFeed(utensil, foodSpoon, args.User, args.User);
            return;
        }
        else if (!HasComp<MobStateComponent>(args.Target))
            return;

        utensil.Comp.LastFood = null;

        args.Handled = true;

        ScoopableFeed(utensil, foodSpoon, args.User, args.Target.Value);
    }

    private void AfterConsume(Entity<RMCFoodScoopingComponent> utensil, ref ConsumeDoAfterEvent args)
    {
        //Only go through on success
        if (!args.Handled || TerminatingOrDeleted(utensil.Comp.LastFood))
        {
            utensil.Comp.LastFood = null;
            return;
        }

        if (utensil.Comp.LastFood == null || !TryComp<FoodComponent>(utensil, out var foodSpoon))
            return;

        TryScoopFood(utensil, args.User, utensil.Comp.LastFood.Value, false);
        ScoopableFeed(utensil, foodSpoon, args.User, args.User, false);
    }

    private void ScoopableFeed(Entity<RMCFoodScoopingComponent> utensil, FoodComponent food, EntityUid user, EntityUid target, bool message = true)
    {
        if (!_solu.TryGetSolution(utensil.Owner, utensil.Comp.SpoonSolution, out var solc))
            return;

        if (solc.Value.Comp.Solution.Volume == 0)
            return;

        if (_food.TryFeed(user, target, utensil, food, true).Success && message)
        {
            if (target == user)
                _popup.PopupEntity(Loc.GetString("rmc-scoop-feed-self", ("user", user), ("food", utensil.Comp.FoodName), ("tool", utensil)), user);
            else
                _popup.PopupEntity(Loc.GetString("rmc-scoop-feed-other", ("user", user), ("target", target), ("food", utensil.Comp.FoodName), ("tool", utensil)), user);
        }
    }
}
