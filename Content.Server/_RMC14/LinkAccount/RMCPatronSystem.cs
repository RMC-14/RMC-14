using Content.Server.GameTicking;
using Content.Shared._RMC14.Patron;
using Robust.Shared.Network;

namespace Content.Server._RMC14.LinkAccount;

public sealed class RMCPatronSystem : EntitySystem
{
    [Dependency] private readonly LinkAccountManager _linkAccount = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<GameRunLevelChangedEvent>(OnGameRunLevelChanged);
        ReloadPatrons();
    }

    private void OnGameRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        switch (ev.New)
        {
            case GameRunLevel.PreRoundLobby:
            case GameRunLevel.PostRound:
                ReloadPatrons();
                break;
        }
    }

    private async void ReloadPatrons()
    {
        try
        {
            await _linkAccount.RefreshAllPatrons();
            _linkAccount.SendPatronsToAll();
        }
        catch (Exception e)
        {
            Log.Error($"Error reloading Patrons list: {e}");
        }
    }
}
