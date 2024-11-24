using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class CustomInputField : InputField
{
    public Canvas ParentCanvas;

    protected override void Start()
    {
        base.Start();
        this.shouldActivateOnSelect = false;
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (!this.interactable)
            return;
        base.OnPointerClick(eventData);
        StartCoroutine(SetCaretPositionNextFrame(eventData));
    }

    private IEnumerator SetCaretPositionNextFrame(PointerEventData eventData)
    {
        if (this.isFocused)
            yield break;
        UpdateLabel();

        Color originColor = selectionColor;
        selectionColor = new Color { a = 0 };

        yield return null;

        Vector2 localMousePos;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(textComponent.rectTransform, eventData.pointerPressRaycast.screenPosition, eventData.pressEventCamera, out localMousePos);
        caretSelectPositionInternal = caretPositionInternal = GetCharacterIndexFromPosition(localMousePos) + m_DrawStart;
        selectionColor = originColor;
        UpdateLabel();
    }
}