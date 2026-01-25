using Content.Server.Light.EntitySystems;
using Content.Shared._RMC14.Xenonids.Doom;
using Content.Shared.Light.Components;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;

namespace Content.Server._RMC14.Xenonids.Doom;
public sealed partial class XenoDoomSystem : SharedXenoDoomSystem
{
    [Dependency] private readonly ExpendableLightSystem _expend = default!;
    [Dependency] private readonly ContainerSystem _container = default!;


    private readonly HashSet<Entity<PointLightComponent>> _lights = new();

    protected override void OnDoomedLightAdded(Entity<LightDoomedComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp<PointLightComponent>(ent, out var light))
            return;

        //Flares that are not held/in containers
        //Because too evil
        if (TryComp<ExpendableLightComponent>(ent, out var flare) &&
            !_container.IsEntityInContainer(ent))
        {
            //Make it burn out quick with no light
            flare.StateExpiryTime = 0;
            flare.GlowDuration = TimeSpan.FromSeconds(0);
            flare.FadeOutDuration = TimeSpan.FromSeconds(0);

            //So off flares burn out - cause thats what happens
            _expend.TryActivate((ent, flare));

            ent.Comp.WasEnabled = false;
            RemCompDeferred<LightDoomedComponent>(ent);
            return;
        }

        ent.Comp.WasEnabled = light.Enabled;
        if (!light.Enabled)
            ent.Comp.DoomActivated = true;

        base.OnDoomedLightAdded(ent, ref args);
        Dirty(ent);
    }
    protected override void OnXenoDoomAction(Entity<XenoDoomComponent> xeno, ref XenoDoomActionEvent args)
    {
        base.OnXenoDoomAction(xeno, ref args);

        _lights.Clear();
        _entityLookup.GetEntitiesInRange(Transform(xeno).Coordinates, xeno.Comp.Range, _lights);

        foreach (var light in _lights)
        {
            //Note this used to check for unoccluded but for lights that can be on walls that makes it kinda fail
            //So while this is a bit of a buff its king, so it shouldn't matter much
            if (!EnsureComp<WaitingDoomComponent>(light, out var waiting))
            {
                var origin = _transform.GetMapCoordinates(xeno);
                var target = _transform.GetMapCoordinates(light);
                var diff = target.Position - origin.Position;
                waiting.DoomAt = TimeSpan.FromSeconds(diff.Length()) * xeno.Comp.ExtinguishTimePerDistanceMult;
                Dirty(light);
            }
        }
    }
}
