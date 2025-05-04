using Content.Server.GameTicking;
using Content.Shared._RMC14.Xenonids.Name;
using Content.Shared.GameTicking;
using Content.Shared.NameModifier.EntitySystems;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Name;

public sealed class XenoNameSystem : SharedXenoNameSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
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
            var profile = _gameTicker.GetPlayerProfile(actor.PlayerSession);
            var name = EnsureComp<XenoNameComponent>(xeno);
            name.Prefix = profile.XenoPrefix;
            name.Number = _available.Count == 0 ? _random.Next(1, 1000) : _random.PickAndTake(_available);
            name.Postfix = profile.XenoPostfix;
            _nameModifier.RefreshNameModifiers(xeno);
            RemCompDeferred<AssignXenoNameComponent>(xeno);
        }
        catch (Exception e)
        {
            Log.Error($"Error setting up xeno name for {ToPrettyString(xeno)}:\n{e}");
        }
    }
}
