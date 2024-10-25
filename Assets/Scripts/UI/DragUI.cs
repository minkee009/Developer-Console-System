using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SPTr.UI
{
    public class DragUI : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
    {
        [Header("참조 컴포넌트")]
        public RectTransform MoveableRect;
        public Canvas ParentCanvas;

        private Vector2 _internalPosition;
        private Vector2 _currentCanvasResolution => ParentCanvas.renderingDisplaySize / ParentCanvas.scaleFactor;

        public void Awake()
        {
            _internalPosition = MoveableRect.anchoredPosition;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            MoveableRect.SetAsLastSibling();
        }

        public void OnDrag(PointerEventData eventData)
        {
            _internalPosition += eventData.delta / ParentCanvas.scaleFactor;

            //경계를 넘었는지 확인
            var currentMaxPos = CalcMaxPosition(_internalPosition);

            var clampedPosX = _internalPosition.x < 0 
                ? Mathf.Max(_internalPosition.x, 0f) 
                : Mathf.Min(currentMaxPos.x, _currentCanvasResolution.x) - MoveableRect.sizeDelta.x;

            var clampedPosY = _internalPosition.y > 0 
                ? Mathf.Min(_internalPosition.y, 0f)
                : Mathf.Max(currentMaxPos.y, -_currentCanvasResolution.y) + MoveableRect.sizeDelta.y;

            MoveableRect.anchoredPosition = new Vector2(clampedPosX, clampedPosY);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            //내부 위치 덮어쓰기
            _internalPosition = MoveableRect.anchoredPosition;
        }

        private Vector2 CalcMaxPosition(Vector2 anchoredVec) => new Vector2(anchoredVec.x + MoveableRect.sizeDelta.x, anchoredVec.y - MoveableRect.sizeDelta.y);
    }
}


