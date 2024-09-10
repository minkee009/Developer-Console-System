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
        [Header("���� ������Ʈ")]
        public RectTransform ResizeableRect;
        public Canvas ParentCanvas;

        [Header("�⺻ ��")]
        public float minWidth = 600.0f;
        public float minHeight = 500.0f;

        private Vector2 _internalPosition;
        private Vector2 _clickOffset;
        private RectTransform _rect;

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
            ClampRectSize();
        }

        public void ClampRectSize()
        {
            ResizeableRect.sizeDelta = new Vector2(Mathf.Max(minWidth, ResizeableRect.sizeDelta.x), Mathf.Max(minHeight, ResizeableRect.sizeDelta.y));
        }

    }
}
