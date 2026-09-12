using Content.Server.Power.Components;
using Content.Shared._RMC14.Power;
using Content.Shared.Placeable;
using Content.Shared.Temperature;
using Content.Shared.Temperature.Components;
using Content.Shared.Temperature.Systems;

namespace Content.Server.Temperature.Systems;

/// <summary>
/// Handles the server-only parts of <see cref="SharedEntityHeaterSystem"/>
/// </summary>
public sealed class EntityHeaterSystem : SharedEntityHeaterSystem
{
    [Dependency] private readonly TemperatureSystem _temperature = default!;

    private float _grillpower; //SurfinNinja- HOW THE HELL DOES THIS SPAGHETTI WORK? GOD I WISH I KNEW HOW TO CODE

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityHeaterComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<EntityHeaterComponent> ent, ref MapInitEvent args)
    {
        // Set initial power level
        if (TryComp<RMCPowerReceiverComponent>(ent, out var power)) //Changed for RMC
            _grillpower = SettingPower(ent.Comp.Setting, ent.Comp.Power);
    }

    public override void Update(float deltaTime)
    {
        var query = EntityQueryEnumerator<EntityHeaterComponent, ItemPlacerComponent, RMCPowerReceiverComponent>(); //Changed for RMC
        while (query.MoveNext(out var entity, out var settingPower, out var placer, out _))
        {
            if (settingPower.Setting == EntityHeaterSetting.Off) //Changed for RMC
                continue;

            // don't divide by total entities since it's a big grill
            // excess would just be wasted in the air but that's not worth simulating
            // if you want a heater thermomachine just use that...
            var energy = _grillpower * deltaTime; //Changed for RMC
            foreach (var ent in placer.PlacedEntities)
            {
                _temperature.ChangeHeat(ent, energy);
            }
        }
    }

    /// <remarks>
    /// <see cref="ApcPowerReceiverComponent"/> doesn't exist on the client, so we need
    /// this server-only override to handle setting the network load.
    /// </remarks>
    protected override void ChangeSetting(Entity<EntityHeaterComponent> ent, EntityHeaterSetting setting, EntityUid? user = null)
    {
        base.ChangeSetting(ent, setting, user);

        if (!TryComp<RMCPowerReceiverComponent>(ent, out var power)) //Changed for RMC
            return;

        _grillpower = (int)SettingPower(setting, 2400f); //Changed for RMC
    }
}
