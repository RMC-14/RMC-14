using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Stun;
using Content.Shared.CombatMode;
using Content.Shared.Damage;
using Content.Shared.Interaction;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCSmallAnimalSystem : RMCAnimalSystem
{
    [Dependency] private readonly RMCSizeStunSystem _size = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCBunnyComponent, MapInitEvent>(OnBunnyMapInit);
        SubscribeLocalEvent<RMCBunnyComponent, InteractHandEvent>(OnBunnyInteractHand, before: [typeof(InteractionPopupSystem)]);
        SubscribeLocalEvent<RMCBunnyComponent, DisarmedEvent>(OnBunnyDisarmed);
        SubscribeLocalEvent<RMCBunnyComponent, DamageChangedEvent>(OnBunnyDamageChanged);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = Timing.CurTime;
        UpdateBunnies(now);
    }
}
