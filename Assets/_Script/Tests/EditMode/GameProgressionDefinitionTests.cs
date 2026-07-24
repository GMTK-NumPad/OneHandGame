using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// 전체 진행 순서와 실제 진입 시점의 스테이지 데이터 검증을 검사합니다.
/// </summary>
public sealed class GameProgressionDefinitionTests
{
    /// <summary>
    /// 아직 라운드가 없는 이후 스테이지가 튜토리얼 시작을 막지 않고 해당 스테이지 진입만 실패시키는지 검사합니다.
    /// </summary>
    [Test]
    public void IncompleteFutureStage_DoesNotInvalidateSequence()
    {
        GameProgressionDefinition progression =
            ScriptableObject.CreateInstance<GameProgressionDefinition>();
        StageDefinition tutorial =
            ScriptableObject.CreateInstance<StageDefinition>();
        StageDefinition stageA =
            ScriptableObject.CreateInstance<StageDefinition>();
        var rounds = new List<RoundDefinition>();

        try
        {
            for (int index = 0; index < 3; index++)
            {
                rounds.Add(
                    ScriptableObject.CreateInstance<RoundDefinition>());
            }

            SetField(tutorial, "rounds", rounds);
            SetField(stageA, "stageId", GameStageId.A);
            SetField(
                progression,
                "stages",
                new List<StageDefinition>
                {
                    tutorial,
                    stageA
                });

            Assert.That(
                progression.IsValidSequence(),
                Is.True);
            Assert.That(
                progression.TryGetStage(0, out _),
                Is.True);
            Assert.That(
                progression.TryGetStage(1, out _),
                Is.False);
        }
        finally
        {
            foreach (RoundDefinition round in rounds)
            {
                Object.DestroyImmediate(round);
            }

            Object.DestroyImmediate(stageA);
            Object.DestroyImmediate(tutorial);
            Object.DestroyImmediate(progression);
        }
    }

    /// <summary>
    /// 테스트용 SO의 비공개 직렬화 필드에 값을 지정합니다.
    /// </summary>
    private static void SetField(
        object target,
        string fieldName,
        object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance
            | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }
}
