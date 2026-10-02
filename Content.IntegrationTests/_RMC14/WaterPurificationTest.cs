using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Water;
using Content.Shared.Coordinates;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests._RMC14;

[TestFixture]
public sealed class WaterPurificationTest
{
    [TestPrototypes]
    private const string Prototypes = """
        - type: entity
          id: RMCWaterPurificationTestAlreadyPurified
          parent: RMCDesertWaterPurifiableShallowFilterOne
          components:
          - type: PurifiableWater
            state: Purified
        """;

    [TestCase("RMCDesertWaterPurifiableShallowFilterOne")]
    [TestCase("RMCDesertWaterPurifiableDeepFilterOne")]
    public async Task PurificationRemovesBothHazards(string prototype)
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entities = server.EntMan;
        var waterSystem = server.System<RMCWaterSystem>();
        var map = await pair.CreateTestMap();
        EntityUid water = default;
        EntityUid permanentWater = default;

        await server.WaitAssertion(() =>
        {
            water = entities.SpawnAtPosition(prototype, map.Grid.Owner.ToCoordinates());
            permanentWater = entities.SpawnAtPosition("RMCToxicWaterShallow", map.Grid.Owner.ToCoordinates());
            Assert.That(entities.HasComponent<DamageOverTimeComponent>(water), Is.True);
            Assert.That(entities.HasComponent<VehicleCorrosiveTileComponent>(water), Is.True);
            Assert.That(waterSystem.StartPurification(water), Is.True);
        });

        await pair.RunSeconds(3);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entities.GetComponent<PurifiableWaterComponent>(water).State,
                    Is.EqualTo(PurifiableWaterState.Purified));
                Assert.That(entities.HasComponent<DamageOverTimeComponent>(water), Is.False);
                Assert.That(entities.HasComponent<VehicleCorrosiveTileComponent>(water), Is.False);
                Assert.That(entities.HasComponent<DamageOverTimeComponent>(permanentWater), Is.True);
                Assert.That(entities.HasComponent<VehicleCorrosiveTileComponent>(permanentWater), Is.True);
            });
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task AlreadyPurifiedWaterAndVisualShoreHaveNoHazards()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entities = server.EntMan;
        var map = await pair.CreateTestMap();
        EntityUid water = default;
        EntityUid shore = default;

        await server.WaitPost(() =>
        {
            water = entities.SpawnAtPosition("RMCWaterPurificationTestAlreadyPurified", map.Grid.Owner.ToCoordinates());
            shore = entities.SpawnAtPosition("RMCDesertWaterPurifiableShoreCornerFilterOne", map.Grid.Owner.ToCoordinates());
        });

        await pair.RunTicksSync(1);

        await server.WaitAssertion(() =>
        {
            Assert.Multiple(() =>
            {
                Assert.That(entities.HasComponent<DamageOverTimeComponent>(water), Is.False);
                Assert.That(entities.HasComponent<VehicleCorrosiveTileComponent>(water), Is.False);
                Assert.That(entities.HasComponent<DamageOverTimeComponent>(shore), Is.False);
                Assert.That(entities.HasComponent<VehicleCorrosiveTileComponent>(shore), Is.False);
            });
        });

        await pair.CleanReturnAsync();
    }
}
