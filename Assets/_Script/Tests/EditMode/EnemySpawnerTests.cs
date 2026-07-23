using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 몬스터 스포너가 논리 좌표, 보드 점유와 런타임 상태를 함께 설정하는지 검사합니다.
/// </summary>
public sealed class EnemySpawnerTests
{
    /// <summary>
    /// 지정한 보드 좌표에 생성된 몬스터가 월드 좌표와 런타임 상태를 올바르게 갖는지 검사합니다.
    /// </summary>
    [Test]
    public void TrySpawnEnemy_PlacesAndInitializesEnemy()
    {
        GameObject boardObject = new("Board");
        GameObject spawnerObject = new("EnemySpawner");
        GameObject playerObject = new("Player");
        GameObject prefabObject = new("EnemyPrefab");
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();
        GoldRewardRulesDefinition rewardRules =
            ScriptableObject.CreateInstance<GoldRewardRulesDefinition>();

        try
        {
            BoardManager boardManager =
                boardObject.AddComponent<BoardManager>();
            EnemySpawner spawner =
                spawnerObject.AddComponent<EnemySpawner>();
            BoardActor playerActor =
                playerObject.AddComponent<BoardActor>();
            prefabObject.AddComponent<BoardActor>();
            EnemyActor enemyPrefab =
                prefabObject.AddComponent<EnemyActor>();
            var spawnPosition = new GridPosition(5, 3);

            spawner.Configure(boardManager, rewardRules);
            boardManager.TryPlaceActor(
                playerActor,
                boardManager.CenterPosition);
            spawner.SetPlayerActor(playerActor);

            bool succeeded = spawner.TrySpawnEnemy(
                enemyPrefab,
                definition,
                spawnPosition,
                out EnemyActor spawnedEnemy);

            Assert.That(succeeded, Is.True);
            Assert.That(spawnedEnemy, Is.Not.Null);
            Assert.That(spawnedEnemy.IsInitialized, Is.True);
            Assert.That(spawnedEnemy.Definition, Is.SameAs(definition));
            Assert.That(
                spawnedEnemy.RuntimeState.InstanceId,
                Is.EqualTo(spawnedEnemy.gameObject.GetInstanceID()));
            Assert.That(
                spawnedEnemy.BoardActor.Position,
                Is.EqualTo(spawnPosition));
            Assert.That(
                spawnedEnemy.transform.position,
                Is.EqualTo(boardManager.BoardToWorld(spawnPosition)));
            Assert.That(
                boardManager.TryGetActor(
                    spawnPosition,
                    out BoardActor registeredActor),
                Is.True);
            Assert.That(
                registeredActor,
                Is.SameAs(spawnedEnemy.BoardActor));

            spawner.ClearSpawnedEnemies();
        }
        finally
        {
            Object.DestroyImmediate(prefabObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(spawnerObject);
            Object.DestroyImmediate(boardObject);
            Object.DestroyImmediate(definition);
            Object.DestroyImmediate(rewardRules);
        }
    }
}
