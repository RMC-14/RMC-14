using Content.Shared.Humanoid;
using Content.Shared.Preferences;

namespace Content.Server._RMC14.Name;

public sealed class RandomizeNameSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomizeNameComponent, ComponentStartup>(OnStartup);
    }

    public void OnStartup(Entity<RandomizeNameComponent> ent, ref ComponentStartup args)
    {
        if (TryComp<HumanoidAppearanceComponent>(ent, out var appearance))
        {
            var name = HumanoidCharacterProfile.GetName(appearance.Species, appearance.Gender);
            _metaData.SetEntityName(ent, name);
        }

        RemComp<RandomizeNameComponent>(ent);
    }
}
