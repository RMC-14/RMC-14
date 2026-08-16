using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests.Chemistry;

[TestFixture]
public sealed class ES11FuelCanisterTest
{
    [Test]
    public async Task FullCanisterRefillsExactlyThreeStandardWelders()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var testMap = await pair.CreateTestMap();
        var entityManager = server.ResolveDependency<IEntityManager>();
        var solutionSystem = entityManager.System<SharedSolutionContainerSystem>();

        await server.WaitAssertion(() =>
        {
            var canister = entityManager.SpawnEntity("CMES11MobileFuelCanister", testMap.GridCoords);
            Assert.That(solutionSystem.TryGetSolution(canister, "tank", out _, out var fuel));
            Assert.That(fuel.Volume, Is.EqualTo(FixedPoint2.New(300)));

            for (var i = 0; i < 4; i++)
            {
                var welder = entityManager.SpawnEntity("CMWelder", testMap.GridCoords);
                Assert.That(solutionSystem.TryGetSolution(welder, "Welder", out var welderSolution, out var welderFuel));
                Assert.That(solutionSystem.RemoveReagent(welderSolution.Value, "WeldingFuel", FixedPoint2.New(100)));

                var transferred = solutionSystem.TryTransferSolution(welderSolution.Value, fuel, FixedPoint2.New(100));

                Assert.That(welderFuel.Volume, Is.EqualTo(FixedPoint2.New(i < 3 ? 100 : 0)));
                Assert.That(fuel.Volume, Is.EqualTo(FixedPoint2.New(Math.Max(0, 200 - i * 100))));
                Assert.That(transferred, Is.EqualTo(i < 3));
            }
        });

        await pair.CleanReturnAsync();
    }
}
