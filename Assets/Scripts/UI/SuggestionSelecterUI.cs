using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SuggestionSelecterUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler, IPointerDownHandler, IPointerUpHandler
{
    public bool isListed => _ownImage.enabled;

    [Header("참조 컴포넌트")]
    public Canvas ParentCanvas;
    public RectTransform SelecterRect;
    public Image SeleterImage;

    [Header("기본 값")]
    public int yPadding = 4;
    public int elementHeight = 16;
    public Color hoveringColor;
    public Color pressedColor;

    [Header("이벤트")]
    public UnityEvent<int> OnClick;

    private Image _ownImage;
    private RectTransform _ownRect;
    private bool _isPointerDetected;
    private bool _isPointerDown;
    private int _currentIndex;
    private const int MAX_SUGGESTION_IDX = 14;

    private void Awake()
    {
        _ownImage = GetComponent<Image>();
        _ownRect = GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isListed)
            SelecterRect.gameObject.SetActive(false);
        else
            SelecterRect.sizeDelta = new Vector2(_ownRect.sizeDelta.x, elementHeight);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isListed)
        {
            SelecterRect.gameObject.SetActive(true);
            _isPointerDetected = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isListed)
        {
            SelecterRect.gameObject.SetActive(_isPointerDown);
            _isPointerDetected = false;
        }
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if(!_isPointerDetected || _isPointerDown)
            return;

        int scaledElemetH = (int)(elementHeight * ParentCanvas.scaleFactor);
        int scaledYPadding = (int)(yPadding * ParentCanvas.scaleFactor);

        int maxIndex = ((int)(_ownRect.sizeDelta.y * ParentCanvas.scaleFactor) / scaledElemetH) - 1;
        _currentIndex = Math.Min(maxIndex, (int)(_ownRect.position.y - eventData.position.y - scaledYPadding) / scaledElemetH);

        SelecterRect.position = (_ownRect.position + (Vector3.down * (_currentIndex * scaledElemetH + scaledYPadding)));

        if (_currentIndex > MAX_SUGGESTION_IDX)
        {
            _currentIndex = -1;
            SeleterImage.color = new Color(0, 0, 0, 0);
        }
        else
            SeleterImage.color = hoveringColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (_currentIndex == -1)
            return;

        SeleterImage.color = pressedColor;
        _isPointerDown = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (_currentIndex != -1)
            OnClick?.Invoke(_currentIndex);

        _isPointerDown = false;
    }
}
