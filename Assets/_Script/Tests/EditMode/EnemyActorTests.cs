using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 몬스터 피해, 처치 이벤트와 보드 점유 해제 흐름을 검사합니다.
/// </summary>
public sealed class EnemyActorTests
{
    /// <summary>
    /// 몬스터가 처음 처치될 때 한 번만 알리고 점유하던 타일을 비우는지 검사합니다.
    /// </summary>
    [Test]
    public void LethalDamage_ReleasesBoardAndReportsOnce()
    {
        GameObject boardObject = new("Board");
        GameObject enemyObject = new("Enemy");
        EnemyDefinition definition =
            ScriptableObject.CreateInstance<EnemyDefinition>();

        try
        {
            BoardManager boardManager =
                boardObject.AddComponent<BoardManager>();
            EnemyActor enemy =
                enemyObject.AddComponent<EnemyActor>();
            var position = new GridPosition(4, 3);
            int defeatedEventCount = 0;

            enemy.Initialize(
                definition,
                new GoldRewardResult(10, isJackpot: false));
            boardManager.TryPlaceActor(
                enemy.BoardActor,
                position);
            enemy.Defeated += _ => defeatedEventCount++;

            EnemyDamageResult firstHit =
                enemy.TakeDamage(1);
            EnemyDamageResult lethalHit =
                enemy.TakeDamage(1);
            EnemyDamageResult repeatedHit =
                enemy.TakeDamage(1);

            Assert.That(firstHit.AppliedDamage, Is.EqualTo(1));
            Assert.That(firstHit.DidDefeat, Is.False);
            Assert.That(lethalHit.AppliedDamage, Is.EqualTo(1));
            Assert.That(lethalHit.DidDefeat, Is.True);
            Assert.That(repeatedHit.AppliedDamage, Is.EqualTo(0));
            Assert.That(repeatedHit.DidDefeat, Is.False);
            Assert.That(defeatedEventCount, Is.EqualTo(1));
            Assert.That(boardManager.CanEnter(position), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyObject);
            Object.DestroyImmediate(boardObject);
            Object.DestroyImmediate(definition);
        }
    }
}
