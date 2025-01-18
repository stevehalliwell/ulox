using NUnit.Framework;

namespace ULox.Core.Tests
{
    [TestFixture]
    public class LimitTests : EngineTestBase
    {
        [Test]
        public void Compile_WhenManyConstantsAndLabels_ShouldPass()
        {
            testEngine.Run(@"
fun GameUIUpdate(dt)
{
    var playerShip = world.playerShip;

    playerPipRankSystem.scalingState = playerShip.scalingState;
    playerPipRankSystem.Tick(dt);
    livesProgressData.Tick(dt);
    enemySpawner1.spawnCentreX = playerShip.posX;
    enemySpawner1.spawnCentreY = playerShip.posY;
    enemySpawner1.Tick(dt);
    enemySpawner2.spawnCentreX = playerShip.posX;
    enemySpawner2.spawnCentreY = playerShip.posY;
    enemySpawner2.Tick(dt);
    enemySpawner3.spawnCentreX = playerShip.posX;
    enemySpawner3.spawnCentreY = playerShip.posY;
    enemySpawner3.Tick(dt);
    
    if(PlayerMenuInput.IsMenuPressed())
    {
        gameStateSystem.PushState(gameOverlayStateBindings);
    }

    PlayerShipInput.Tick(playerShip.controlState);
	ProfileBegin(""EnemyAI"");
    loop world.allDogFightAISets { EnemyShipDogFightAI.Tick(item, world.playerShips, dt); }
    EnemyShipAvoid.Tick(world.enemyShips, dt);
    loop world.allEnemyShipSets { EnemyBoundarySteerSystem.Tick(world.waterBoundary, item, dt); }
    loop world.allEnemyFlyBySets { EnemyShipFlyByAI.Tick(item, world.playerShips, dt); }
	ProfileEnd(""EnemyAI"");
	ProfileBegin(""ShipMovementSystem"");
    loop world.allShipSets { ShipMovementSystem.Tick(item, dt); }
	ProfileEnd(""ShipMovementSystem"");
    
	ProfileBegin(""PipSystem"");
    pipPlayerInteractionSystem.playerPosX = playerShip.posX;
    pipPlayerInteractionSystem.playerPosY = playerShip.posY;
    pipPlayerInteractionSystem.pickupRange = playerShip.shipChar.pipPickupRange;
    pipPlayerInteractionSystem.pipAttractRange = playerShip.shipChar.pipAttractRange;
    pipPlayerInteractionSystem.pipAttractForce = playerShip.shipChar.pipAttractForce;
    pipPlayerInteractionSystem.Tick(dt);
    PipSystem.Tick(world.pipData, waterLineData, dt);
    loop world.allProjectileSets { PipBulletInteractionSystem.Tick(world.pipData, item, dt); }
	ProfileEnd(""PipSystem"");

	ProfileBegin(""ShipWeaponSystem"");
    loop world.allShipSets { ShipWeaponSystem.Tick(item, waterLineData, dt); }
    {
        var spreadRange = Math.Lerp(playerShip.shipChar.projectileSpreadMin, playerShip.shipChar.projectileSpreadMax, playerShip.weaponState.stabilityCounter / playerShip.shipChar.projectileSpreadTime);
        var playerFireCone = spreadRange * 2;
        playerFireCone = Math.Max(2, playerFireCone);
        var playerFireSize = 2; //todo should be based on speed or size
        var playerNormlisedFireTime = Math.Clamp(0,1,1-(playerShip.weaponState.fireRateCounter / playerShip.shipChar.fireRate));
        SetFiringConeView(playerShip.go, playerFireCone, playerFireSize, playerNormlisedFireTime);
    }
	ProfileEnd(""ShipWeaponSystem"");
    shipLeadingCamera.Tick(playerShip, dt);

	ProfileBegin(""Boundaries"");
    loop world.allBoundaries { PlayerBoundarySystem.Tick(item, world.playerShips, dt); }
    loop world.allEnemyShipSets { EnemyBoundarySystem.Tick(world.waterBoundary, item, dt); }
	ProfileEnd(""Boundaries"");
	ProfileBegin(""Projectiles"");
    loop world.allProjectileSets { LifeTimeSystem.Tick(item, dt); }
    MissileTargetSelection.Tick(world.playerMissileData, world.enemyShips, dt);
    MissileTargetSelection.Tick(world.playerProNavData, world.enemyShips, dt);
    MissileRotateTowardsTarget.Tick(world.playerMissileData, world.enemyShips, dt);
    MissileProNavTarget.Tick(world.playerProNavData, world.enemyShips, dt);
    loop world.allProjectileSets 
    { 
        BulletSystem.Tick(item, dt);
        VelocitySystem.Tick(item, dt);
    }
	ProfileEnd(""Projectiles"");
    
	ProfileBegin(""WaterInteractions"");
    loop world.allProjectileSets { BulletWaterInteractionSystem.Tick(item, waterLineData, dt); }
    loop world.allShipSets { ShipWaterInteractionSystem.Tick(item, waterLineData, dt); }
	ProfileEnd(""WaterInteractions"");
	ProfileBegin(""WaterLineSystem"");
    waterLineData.focusPointX = playerShip.posX;
    WaterLineSystem.Tick(waterLineData, dt);
    ProfileEnd(""WaterLineSystem"");
	ProfileBegin(""BulletInteractions"");
    loop world.allPlayerProjectileSets { loop world.allEnemyShipSets, j { BulletShipInteractionSystem.Tick(item, jtem); } }
    loop world.allEnemyProjectileSets { BulletShipInteractionSystem.Tick(item, world.playerShips); }
	ProfileEnd(""BulletInteractions"");
    ProfileBegin(""ShipCollisionSystem"");
    ShipShipInteractionSystem.Tick(world.playerShips, world.enemyShips, dt);
    ProfileEnd(""ShipCollisionSystem"");
	ProfileBegin(""ShipHealthSystems"");
    playerShipHealthRegenSystem.Tick(playerShip, dt);
    loop world.allEnemyShipSets { ShipHealthSystem.EnemyTick(item, EnemyDeathHandler, dt); }
    ShipHealthSystem.PlayerTick(playerShip, PlayerDeathHandler, dt);
	ProfileEnd(""ShipHealthSystems"");

	ProfileBegin(""UI"");
    ShipDataUISystem.Tick(shipDataUISystemData, playerShip);
    GameLivesUISystem.Tick(playerPipRankSystem, livesProgressData);
    PlayerHealthUISystem.Tick(playerShip);
	ProfileEnd(""UI"");
}");

            Assert.AreEqual("", testEngine.InterpreterResult);
        }
    }
}