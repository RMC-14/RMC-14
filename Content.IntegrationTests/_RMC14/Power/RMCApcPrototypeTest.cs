using System.Numerics;
using Content.Server.Construction.Components;
using Content.Shared._RMC14.Power;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;

namespace Content.IntegrationTests._RMC14.Power;

[TestFixture]
public sealed class RMCApcPrototypeTest
{
    [Test]
    public async Task ConstructedApcDoesNotSpawnFreeStartingCell()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var apc = entMan.SpawnEntity("CMApcConstructed", new MapCoordinates(Vector2.Zero, map.MapId));
            var comp = entMan.GetComponent<RMCApcComponent>(apc);
            var construction = entMan.GetComponent<ConstructionComponent>(apc);

            Assert.Multiple(() =>
            {
                Assert.That(comp.StartingCell, Is.Null);
                Assert.That(comp.Cover, Is.EqualTo(RMCApcCover.Open));
                Assert.That(comp.TerminalInstalled, Is.True);
                Assert.That(comp.Electronics, Is.EqualTo(RMCApcElectronics.Secured));
                Assert.That(construction.Graph, Is.EqualTo("CMApc"));
                Assert.That(construction.Node, Is.EqualTo("apc"));
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task MapPlacedApcKeepsNormalStartingCell()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var apc = entMan.SpawnEntity("CMApc", new MapCoordinates(Vector2.Zero, map.MapId));
            var comp = entMan.GetComponent<RMCApcComponent>(apc);

            Assert.That(comp.StartingCell?.Id, Is.EqualTo("RMCPowerCellAPC"));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task ApcFrameItemIsMarkedForRepairs()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var map = await pair.CreateTestMap();
        var entMan = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var frame = entMan.SpawnEntity("CMAPCFrame", new MapCoordinates(Vector2.Zero, map.MapId));

            Assert.That(entMan.HasComponent<RMCApcFrameComponent>(frame), Is.True);
        });

        await pair.CleanReturnAsync();
    }
}
