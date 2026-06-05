using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomScrollRect : ScrollRect
{
#pragma warning disable 0414
    [Header("Custom Settings")]
    [SerializeField] private float _mouseWheelSensitivityMultiplier = 0.002f;
#pragma warning restore 0414 

    public override void OnScroll(PointerEventData eventData)
    {
#if ENABLE_INPUT_SYSTEM
        // 마우스 휠 스크롤 입력인 경우에만 델타값을 제어.
        // (트랙패드나 다른 포인터의 미세 입력과 구분하기 위함)
        if (eventData.scrollDelta.sqrMagnitude > 0.001f)
        {
            Vector2 modifiedDelta = eventData.scrollDelta;
            modifiedDelta.y *= _mouseWheelSensitivityMultiplier;
            modifiedDelta.x *= _mouseWheelSensitivityMultiplier; // 혹시 모를 가로 스크롤 대응
            
            eventData.scrollDelta = modifiedDelta;
        }
#endif

        // 변조된 eventData를 부모(순정 ScrollRect)의 OnScroll로 넘김.
        base.OnScroll(eventData);
    }
}