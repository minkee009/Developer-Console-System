using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SPTr.UI
{
    [RequireComponent(typeof(RectTransform))]
    public class ResizeButton : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        [Header("참조 컴포넌트")]
        public RectTransform ResizeableRect;
        public Canvas ParentCanvas;

        [Header("기본 값")]
        public float minWidth = 600.0f;
        public float minHeight = 500.0f;

        private Vector2 _internalPosition;
        private Vector2 _clickOffset;
        private RectTransform _rect;
        private Vector2 _currentCanvasResolution => ParentCanvas.renderingDisplaySize / ParentCanvas.scaleFactor;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
        }

        private void OnValidate()
        {
            _rect = GetComponent<RectTransform>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            ResizeableRect.SetAsLastSibling();
            _clickOffset = new Vector2(_rect.position.x, _rect.position.y) - eventData.position;
        }


        public void OnDrag(PointerEventData eventData)
        {
            ResizeRect(eventData);
            ClampRectSize();
        }


        public void ResizeRect(PointerEventData eventdata)
        {
            _internalPosition = eventdata.position + _clickOffset;
            ResizeableRect.sizeDelta = new Vector2(_internalPosition.x - ResizeableRect.position.x, ResizeableRect.position.y - _internalPosition.y) / ParentCanvas.scaleFactor;
        }

        public void ClampRectSize()
        {
            var maxSizeDeltaX = (_currentCanvasResolution.x - ResizeableRect.anchoredPosition.x);
            var maxSizeDeltaY = (_currentCanvasResolution.y + ResizeableRect.anchoredPosition.y);

            var clampedSizeDeltaX = Mathf.Min(maxSizeDeltaX, Mathf.Max(minWidth, ResizeableRect.sizeDelta.x));
            var clampedSizeDeltaY = Mathf.Min(maxSizeDeltaY, Mathf.Max(minHeight, ResizeableRect.sizeDelta.y));

            ResizeableRect.sizeDelta = new Vector2(clampedSizeDeltaX, clampedSizeDeltaY);
        }

    }
}
