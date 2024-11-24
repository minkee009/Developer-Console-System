using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.UIElements;

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

    public override void OnUpdateSelected(BaseEventData eventData)
    {
        base.OnUpdateSelected(eventData);

        GUIUtility.systemCopyBuffer = Regex.Replace(GUIUtility.systemCopyBuffer, @"<[^>]+>", string.Empty);
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