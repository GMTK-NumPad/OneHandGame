using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마우스 없이 숫자패드의 이동, 소모품, 특수기와 타게팅 입력을 전달합니다.
/// </summary>
public sealed class NumpadInputController : MonoBehaviour
{
    public event Action<GridPosition> DirectionPressed;
    public event Action<int> ConsumableSlotPressed;
    public event Action<int> SpecialSlotPressed;
    public event Action TargetConfirmed;
    public event Action TargetCancelled;

    public bool InputEnabled { get; set; } = true;

    /// <summary>
    /// 매 프레임 숫자패드에서 새로 눌린 게임 입력 하나를 읽어 전달합니다.
    /// </summary>
    private void Update()
    {
        if (!InputEnabled)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (TryReadDirection(keyboard, out GridPosition direction))
        {
            DirectionPressed?.Invoke(direction);
            return;
        }

        if (keyboard.numpadDivideKey.wasPressedThisFrame)
        {
            ConsumableSlotPressed?.Invoke(0);
            return;
        }

        if (keyboard.numpadMultiplyKey.wasPressedThisFrame)
        {
            ConsumableSlotPressed?.Invoke(1);
            return;
        }

        if (keyboard.numpadMinusKey.wasPressedThisFrame)
        {
            ConsumableSlotPressed?.Invoke(2);
            return;
        }

        if (keyboard.numpad0Key.wasPressedThisFrame)
        {
            SpecialSlotPressed?.Invoke(0);
            return;
        }

        if (keyboard.numpadPeriodKey.wasPressedThisFrame)
        {
            SpecialSlotPressed?.Invoke(1);
            return;
        }

        if (keyboard.numpadEnterKey.wasPressedThisFrame)
        {
            TargetConfirmed?.Invoke();
            return;
        }

        if (keyboard.numpadPlusKey.wasPressedThisFrame)
        {
            TargetCancelled?.Invoke();
        }
    }

    /// <summary>
    /// 숫자패드 1~9에서 중앙 5를 제외한 8방향 입력을 보드 방향으로 변환합니다.
    /// </summary>
    private static bool TryReadDirection(
        Keyboard keyboard,
        out GridPosition direction)
    {
        if (keyboard.numpad1Key.wasPressedThisFrame)
        {
            direction = new GridPosition(-1, -1);
            return true;
        }

        if (keyboard.numpad2Key.wasPressedThisFrame)
        {
            direction = new GridPosition(0, -1);
            return true;
        }

        if (keyboard.numpad3Key.wasPressedThisFrame)
        {
            direction = new GridPosition(1, -1);
            return true;
        }

        if (keyboard.numpad4Key.wasPressedThisFrame)
        {
            direction = new GridPosition(-1, 0);
            return true;
        }

        if (keyboard.numpad6Key.wasPressedThisFrame)
        {
            direction = new GridPosition(1, 0);
            return true;
        }

        if (keyboard.numpad7Key.wasPressedThisFrame)
        {
            direction = new GridPosition(-1, 1);
            return true;
        }

        if (keyboard.numpad8Key.wasPressedThisFrame)
        {
            direction = new GridPosition(0, 1);
            return true;
        }

        if (keyboard.numpad9Key.wasPressedThisFrame)
        {
            direction = new GridPosition(1, 1);
            return true;
        }

        direction = default;
        return false;
    }
}
