using System.Collections.Generic;
using Content.Server._RMC14.Rules;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Presets;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.TacticalMap;
using Content.Shared.CCVar;
using Content.Shared.Movement.Components;
using Robust.Shared.Configuration;

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class PlanetMapLoadTests
{
    [Test]
    public async Task PlanetLoadsWithoutErrorsTest()
    {
        await using var pair = await PoolManager.GetServerClient(new PoolSettings
        {
            Dirty = true,
            DummyTicker = false,
            InLobby = true,
        });

        var server = pair.Server;
        var distress = server.System<CMDistressSignalRuleSystem>();
        var ticker = server.System<GameTicker>();

        var planets = new List<RMCPlanet>();
        await server.WaitPost(() =>
        {
            planets = server.System<RMCPlanetSystem>().GetAllPlanets();
        });

        Assert.That(planets, Is.Not.Empty);
        foreach (var planet in planets)
        {
            Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

            distress.SetPlanet(planet);
            Assert.That(distress.SelectedPlanetMapName, Is.EqualTo(planet.Proto.Name));

            Assert.DoesNotThrowAsync(async () =>
            {
                await pair.WaitCommand("forcepreset CMDistressSignal");
                await PoolManager.WaitUntil(server, () => ticker.RunLevel != GameRunLevel.PreRoundLobby);
            }, $"Failed to load planet {planet.Proto.Name}!");

            await server.WaitPost(() =>
            {
                // https://github.com/RMC-14/RMC-14/actions/runs/19488437482/job/55775559108
                foreach (var allEntity in server.EntMan.AllEntities<InputMoverComponent>())
                {
                    server.EntMan.DeleteEntity(allEntity);
                }
            });

            Assert.Multiple(() =>
            {
                Assert.That(ticker.RunLevel, Is.Not.EqualTo(GameRunLevel.PreRoundLobby));
                Assert.That(server.EntMan.Count<TacticalMapComponent>(), Is.EqualTo(1));
                Assert.That(server.EntMan.Count<RMCPlanetComponent>(), Is.EqualTo(1));
            });

            await pair.WaitCommand("golobby");
            await PoolManager.WaitUntil(server, () => ticker.RunLevel == GameRunLevel.PreRoundLobby);
        }

        await server.WaitIdleAsync();

        var config = server.ResolveDependency<IConfigurationManager>();
        await server.WaitPost(() =>
        {
            config.SetCVar(CCVars.GameLobbyEnabled, false);
            ticker.SetGamePreset((GamePresetPrototype?) null);
            ticker.StartRound();
        });

        await PoolManager.WaitUntil(server, () => ticker.RunLevel != GameRunLevel.PreRoundLobby);

        await pair.CleanReturnAsync();
    }
}
