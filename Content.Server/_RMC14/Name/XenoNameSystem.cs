using Content.Server._RMC14.Preferences;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.GameTicking;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Name;

public sealed class XenoNameSystem : SharedXenoNameSystem
{
    [Dependency] private readonly RMCCharacterSelectionSystem _characters = default!;
    [Dependency] private readonly NameModifierSystem _nameModifier = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly List<int> _available = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestartCleanup);
    }

    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _available.Clear();
        for (var i = 1; i < 1000; i++)
        {
            _available.Add(i);
        }
    }

    public override void SetupName(EntityUid xeno)
    {
        base.SetupName(xeno);

        if (!TryComp(xeno, out ActorComponent? actor))
            return;

        try
        {
            var settings = _characters.GetPlayerSettings(actor.PlayerSession.UserId);
            var name = EnsureComp<XenoNameComponent>(xeno);
            name.Prefix = settings.XenoPrefix;
            name.Number = _available.Count == 0 ? _random.Next(1, 1000) : _random.PickAndTake(_available);
            name.Postfix = settings.XenoPostfix;
            _nameModifier.RefreshNameModifiers(xeno);
            RemCompDeferred<AssignXenoNameComponent>(xeno);
        }
        catch (Exception e)
        {
            Log.Error($"Error setting up xeno name for {ToPrettyString(xeno)}:\n{e}");
        }
    }
}
