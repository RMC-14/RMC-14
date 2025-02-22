using Content.Shared.Examine;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;

namespace Content.Server.Light.EntitySystems
{
    [UsedImplicitly]
    public sealed class RMCExpendableLightSystem : SharedExpendableLightSystem
    {
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly ExpendableLightSystem _light = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ExpendableLightComponent, ExaminedEvent>(OnExpendableLightExamined);
            SubscribeLocalEvent<ExpendableLightComponent, GrenadeContentThrownEvent>(OnGrenadeContentThrown);
        }

        /// <summary>
        ///     Changes the description if light can't be picked up while on.
        /// </summary>
        private void OnExpendableLightExamined(Entity<ExpendableLightComponent> ent, ref ExaminedEvent args)
        {
            if (!ent.Comp.PickupWhileOn && ent.Comp.CurrentState != ExpendableLightState.Dead)
                args.PushMarkup(Loc.GetString("rmc-laser-designator-signal-flare-examine"));
        }

        /// <summary>
        ///     Turns on the light and makes it's body type static if enabled in the component.
        /// </summary>
        private void OnGrenadeContentThrown(Entity<ExpendableLightComponent> ent, ref GrenadeContentThrownEvent args)
        {
            if(!ent.Comp.PickupWhileOn)
                _physics.SetBodyType(ent, BodyType.Static);
            _light.TryActivate((ent.Owner,ent.Comp));
        }
    }
}
