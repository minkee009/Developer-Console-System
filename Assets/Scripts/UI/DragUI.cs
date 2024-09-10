using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SPTr.UI
{
    public class DragUI : MonoBehaviour, IDragHandler
    {
        [Header("참조 컴포넌트")]
        public RectTransform MoveableRect;
        public Canvas ParentCanvas;

        public void OnDrag(PointerEventData eventData)
        {
            MoveableRect.anchoredPosition += eventData.delta / ParentCanvas.scaleFactor;
        }

    }
}


