namespace Content.Shared._RMC14.Lights;

public sealed class PointLightRotationSystem : EntitySystem
{
    [Dependency] private readonly SharedPointLightSystem _pointLight = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<PointLightRotationComponent, ComponentStartup>(OnSetRotation);
        SubscribeLocalEvent<PointLightRotationComponent, MapInitEvent>(OnSetRotation);
        SubscribeLocalEvent<PointLightRotationComponent, AfterAutoHandleStateEvent>(OnSetRotation);
    }

    private void OnSetRotation<T>(Entity<PointLightRotationComponent> ent, ref T args)
    {
        if (_pointLight.TryGetLight(ent, out var light))
#pragma warning disable RA0002
            light.Rotation = ent.Comp.Rotation;
#pragma warning restore RA0002

        Dirty(ent);
    }
}
