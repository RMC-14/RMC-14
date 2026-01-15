using Content.Server.Light.Components;
using Content.Server.Stack;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.IgnitionSource;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Light.Components;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Stacks;
using Content.Shared.Light.EntitySystems;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class ExpendableLightSystem : SharedExpendableLightSystem
    {
        [Dependency] private readonly SharedItemSystem _item = default!;
        [Dependency] private readonly ClothingSystem _clothing = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly SharedAudioSystem _audio = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
        [Dependency] private readonly StackSystem _stackSystem = default!;
        [Dependency] private readonly NameModifierSystem _nameModifier = default!;

        // RMC14
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;

        private static readonly ProtoId<TagPrototype> TrashTag = "Trash";

        public override void Initialize()
        {
            base.Initialize();

            // RMC14
            SubscribeLocalEvent<ExpendableLightComponent, MapInitEvent>(OnExpLightInit);
            // RMC14

            SubscribeLocalEvent<ExpendableLightComponent, UseInHandEvent>(OnExpLightUse);
            SubscribeLocalEvent<ExpendableLightComponent, GetVerbsEvent<ActivationVerb>>(AddIgniteVerb);
            SubscribeLocalEvent<ExpendableLightComponent, InteractUsingEvent>(OnInteractUsing);
            SubscribeLocalEvent<ExpendableLightComponent, RefreshNameModifiersEvent>(OnRefreshNameModifiers);
        }

        public override void Update(float frameTime)
        {
            var query = EntityQueryEnumerator<ExpendableLightComponent>();
            while (query.MoveNext(out var uid, out var light))
            {
                UpdateLight((uid, light), frameTime);
            }
        }

        private void UpdateLight(Entity<ExpendableLightComponent> ent, float frameTime)
        {
            var component = ent.Comp;
            if (!component.Activated)
                return;

            component.StateExpiryTime -= frameTime;

            // RMC14
            Dirty(ent);

            if (component.StateExpiryTime <= 0f)
            {
                switch (component.CurrentState)
                {
                    case ExpendableLightState.Lit:
                        component.CurrentState = ExpendableLightState.Fading;
                        component.StateExpiryTime = (float)component.FadeOutDuration.TotalSeconds;

                        UpdateVisualizer(ent);

                        break;

                    default:
                    case ExpendableLightState.Fading:
                        component.CurrentState = ExpendableLightState.Dead;
                        _nameModifier.RefreshNameModifiers(ent.Owner);

                        _tagSystem.AddTag(ent, TrashTag);

                        UpdateSounds(ent);
                        UpdateVisualizer(ent);

                        if (TryComp<ItemComponent>(ent, out var item))
                        {
                            _item.SetHeldPrefix(ent, "unlit", component: item);
                        }

                        // RMC14
                        if (HasComp<PhysicsComponent>(ent))
                            _physics.SetBodyType(ent, BodyType.Dynamic);

                        break;
                }
            }
        }

        /// <summary>
        ///     Enables the light if it is not active. Once active it cannot be turned off.
        /// </summary>
        public bool TryActivate(Entity<ExpendableLightComponent> ent)
        {
            // RMC14
            if (HasComp<FlareSignalComponent>(ent) &&
                HasComp<DropshipTargetComponent>(ent))
            {
                return false;
            }

            var component = ent.Comp;
            if (!component.Activated && component.CurrentState == ExpendableLightState.BrandNew)
            {
                if (TryComp<ItemComponent>(ent, out var item))
                {
                    _item.SetHeldPrefix(ent, "lit", component: item);
                }

                var ignite = new IgnitionEvent(true);
                RaiseLocalEvent(ent, ref ignite);

                component.CurrentState = ExpendableLightState.Lit;

                // RMC14
                Dirty(ent);

                UpdateSounds(ent);
                UpdateVisualizer(ent);
            }
            return true;
        }

        private void OnInteractUsing(EntityUid uid, ExpendableLightComponent component, ref InteractUsingEvent args)
        {
            if (args.Handled)
                return;

            if (!TryComp(args.Used, out StackComponent? stack))
                return;

            if (stack.StackTypeId != component.RefuelMaterialID)
                return;

            if (component.StateExpiryTime + component.RefuelMaterialTime.TotalSeconds >= component.RefuelMaximumDuration.TotalSeconds)
                return;

            if (component.CurrentState is ExpendableLightState.Dead)
            {
                component.CurrentState = ExpendableLightState.BrandNew;
                component.StateExpiryTime = (float)component.RefuelMaterialTime.TotalSeconds;

                _nameModifier.RefreshNameModifiers(uid);
                _stackSystem.SetCount(args.Used, stack.Count - 1, stack);
                UpdateVisualizer((uid, component));
                return;
            }

            component.StateExpiryTime += (float)component.RefuelMaterialTime.TotalSeconds;
            _stackSystem.SetCount(args.Used, stack.Count - 1, stack);
            args.Handled = true;
        }

        private void OnRefreshNameModifiers(Entity<ExpendableLightComponent> entity, ref RefreshNameModifiersEvent args)
        {
            if (entity.Comp.CurrentState is ExpendableLightState.Dead)
                args.AddModifier("expendable-light-spent-prefix");
        }

        private void UpdateVisualizer(Entity<ExpendableLightComponent> ent, AppearanceComponent? appearance = null)
        {
            var component = ent.Comp;
            if (!Resolve(ent, ref appearance, false))
                return;

            _appearance.SetData(ent, ExpendableLightVisuals.State, component.CurrentState, appearance);

            switch (component.CurrentState)
            {
                case ExpendableLightState.Lit:
                    _appearance.SetData(ent, ExpendableLightVisuals.Behavior, component.TurnOnBehaviourID, appearance);
                    break;

                case ExpendableLightState.Fading:
                    _appearance.SetData(ent, ExpendableLightVisuals.Behavior, component.FadeOutBehaviourID, appearance);
                    break;

                case ExpendableLightState.Dead:
                    _appearance.SetData(ent, ExpendableLightVisuals.Behavior, string.Empty, appearance);
                    var ignite = new IgnitionEvent(false);
                    RaiseLocalEvent(ent, ref ignite);
                    break;
            }
        }

        private void UpdateSounds(Entity<ExpendableLightComponent> ent)
        {
            var component = ent.Comp;

            switch (component.CurrentState)
            {
                case ExpendableLightState.Lit:
                    _audio.PlayPvs(component.LitSound, ent);
                    break;
                case ExpendableLightState.Fading:
                    break;
                default:
                    _audio.PlayPvs(component.DieSound, ent);
                    break;
            }

            if (TryComp<ClothingComponent>(ent, out var clothing))
            {
                _clothing.SetEquippedPrefix(ent, component.Activated ? "Activated" : string.Empty, clothing);
            }
        }

        // RMC14
        private void OnExpLightInit(EntityUid uid, ExpendableLightComponent component, MapInitEvent args)
        {
            if (TryComp<ItemComponent>(uid, out var item))
            {
                _item.SetHeldPrefix(uid, "unlit", component: item);
            }

            component.CurrentState = ExpendableLightState.BrandNew;
            component.StateExpiryTime = (float)component.GlowDuration.TotalSeconds;

			// RMC14
            if (component.StartsActivated)
                TryActivate((uid, component));

            // RMC14
            Dirty(uid, component);

            EnsureComp<PointLightComponent>(uid);
        }
        // RMC14

        private void OnExpLightUse(Entity<ExpendableLightComponent> ent, ref UseInHandEvent args)
        {
            if (args.Handled)
                return;

            if (TryActivate(ent))
                args.Handled = true;
        }

        private void AddIgniteVerb(Entity<ExpendableLightComponent> ent, ref GetVerbsEvent<ActivationVerb> args)
        {
            if (!args.CanAccess || !args.CanInteract || HasComp<XenoComponent>(args.User))
                return;

            if (ent.Comp.CurrentState != ExpendableLightState.BrandNew)
                return;

            // Ignite the flare or make the glowstick glow.
            // Also hot damn, those are some shitty glowsticks, we need to get a refund.
            ActivationVerb verb = new()
            {
                Text = Loc.GetString("expendable-light-start-verb"),
                Icon = new SpriteSpecifier.Texture(new ("/Textures/Interface/VerbIcons/light.svg.192dpi.png")),
                Act = () => TryActivate(ent)
            };
            args.Verbs.Add(verb);
        }
    }
}
