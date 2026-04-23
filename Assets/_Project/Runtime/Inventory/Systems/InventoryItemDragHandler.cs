using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheLastTowerDefence.Inventory.Systems
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    [RequireComponent(typeof(InventoryItemView))]
    public sealed class InventoryItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private InventoryItemView _itemView;
        private Canvas _canvas;
        private InventoryGridView _gridView;
        private EquipmentSlotCellView _originSlot;
        private RectTransform _originParent;
        private Vector2 _originAnchoredPosition;
        private Vector2 _originAnchorMin;
        private Vector2 _originAnchorMax;
        private Vector2 _originPivot;
        private RectTransform _dragPlane;
        private Vector2 _dragPointerOffset;
        private int _grabbedCellOffsetX;
        private int _grabbedCellOffsetY;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _canvasGroup = GetComponent<CanvasGroup>();
            _itemView = GetComponent<InventoryItemView>();
            _canvas = GetComponentInParent<Canvas>();
            if (_canvas == null)
            {
                _canvas = FindFirstObjectByType<Canvas>();
            }

            _gridView = FindFirstObjectByType<InventoryGridView>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_rectTransform == null || _canvasGroup == null || _itemView == null)
            {
                return;
            }

            _gridView = FindFirstObjectByType<InventoryGridView>();
            if (_gridView == null)
            {
                return;
            }

            _originParent = _rectTransform.parent as RectTransform;
            _originAnchoredPosition = _rectTransform.anchoredPosition;
            _originAnchorMin = _rectTransform.anchorMin;
            _originAnchorMax = _rectTransform.anchorMax;
            _originPivot = _rectTransform.pivot;
            _originSlot = GetComponentInParent<EquipmentSlotCellView>();
            if (_originSlot != null)
            {
                _originSlot.UnequipIfCurrent(_itemView);
            }

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.85f;
            _dragPlane = _canvas != null ? _canvas.transform as RectTransform : _rectTransform.parent as RectTransform;
            _rectTransform.SetParent(_dragPlane != null ? _dragPlane : _rectTransform.parent, true);

            if (_dragPlane != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _dragPlane,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var pointerLocalOnBegin))
            {
                _dragPointerOffset = _rectTransform.anchoredPosition - pointerLocalOnBegin;
            }
            else
            {
                _dragPointerOffset = Vector2.zero;
            }

            ResolveGrabbedCellOffset(eventData);
            _gridView?.ClearPlacementHighlight();
            ClearAllEquipmentSlotDragHighlights();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_rectTransform == null)
            {
                return;
            }

            var dragPlane = _dragPlane != null ? _dragPlane : (_canvas != null ? _canvas.transform as RectTransform : _rectTransform.parent as RectTransform);
            if (dragPlane == null)
            {
                ClearAllEquipmentSlotDragHighlights();
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragPlane, eventData.position, eventData.pressEventCamera, out var local))
            {
                _rectTransform.anchoredPosition = local + _dragPointerOffset;
            }

            if (_gridView != null && _itemView != null)
            {
                _gridView.UpdatePlacementHighlight(
                    _itemView,
                    eventData.position,
                    eventData.pressEventCamera,
                    _grabbedCellOffsetX,
                    _grabbedCellOffsetY);
            }

            UpdateEquipmentSlotDragHighlights(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_canvasGroup == null || _itemView == null)
            {
                return;
            }

            _gridView?.ClearPlacementHighlight();
            ClearAllEquipmentSlotDragHighlights();

            // Keep raycasts blocked on this item until we resolve the drop; otherwise
            // pointerCurrentRaycast often hits the dragged graphic first and inventory placement fails.
            var dropTarget = FindDropTargetUnderPointer(eventData.position, eventData.pressEventCamera);

            var slot = dropTarget != null ? dropTarget.GetComponentInParent<EquipmentSlotCellView>() : null;
            if (slot != null && slot.TryEquipItem(_itemView))
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
                return;
            }

            var grid = dropTarget != null ? dropTarget.GetComponentInParent<InventoryGridView>() : _gridView;
            if (grid != null && grid.TryPlaceItemByPointer(_itemView, eventData, _grabbedCellOffsetX, _grabbedCellOffsetY))
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
                return;
            }

            if (_originSlot != null &&
                grid != null &&
                grid.ContainsScreenPoint(eventData.position, eventData.pressEventCamera) &&
                grid.TryPlaceItemAtFirstFreeCell(_itemView))
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
                return;
            }

            RestoreOrigin();
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;
        }

        private GameObject FindDropTargetUnderPointer(Vector2 screenPosition, Camera eventCamera)
        {
            if (EventSystem.current == null)
            {
                return null;
            }

            var ped = new PointerEventData(EventSystem.current)
            {
                position = screenPosition,
            };
            var results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(ped, results);
            for (var i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                if (go == null)
                {
                    continue;
                }

                var t = go.transform;
                if (_rectTransform != null && (t == _rectTransform || t.IsChildOf(_rectTransform)))
                {
                    continue;
                }

                return go;
            }

            return null;
        }

        private static void ClearAllEquipmentSlotDragHighlights()
        {
            var slots = FindObjectsByType<EquipmentSlotCellView>(FindObjectsSortMode.None);
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                {
                    slots[i].SetDragEquipHighlight(null);
                }
            }
        }

        private void UpdateEquipmentSlotDragHighlights(PointerEventData eventData)
        {
            ClearAllEquipmentSlotDragHighlights();
            if (_itemView == null || _itemView.Config == null)
            {
                return;
            }

            var go = FindDropTargetUnderPointer(eventData.position, eventData.pressEventCamera);
            var slot = go != null ? go.GetComponentInParent<EquipmentSlotCellView>() : null;
            if (slot != null)
            {
                slot.SetDragEquipHighlight(_itemView.Config);
            }
        }

        private void RestoreOrigin()
        {
            if (_rectTransform == null || _originParent == null)
            {
                return;
            }

            _rectTransform.SetParent(_originParent, false);
            _rectTransform.anchorMin = _originAnchorMin;
            _rectTransform.anchorMax = _originAnchorMax;
            _rectTransform.pivot = _originPivot;
            _rectTransform.anchoredPosition = _originAnchoredPosition;

            if (_originSlot != null)
            {
                _originSlot.TryEquipItem(_itemView);
            }
        }

        private void ResolveGrabbedCellOffset(PointerEventData eventData)
        {
            _grabbedCellOffsetX = 0;
            _grabbedCellOffsetY = 0;

            if (_rectTransform == null || _itemView == null || _itemView.Config == null || _gridView == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _rectTransform,
                    eventData.position,
                    eventData.pressEventCamera,
                    out var localPoint))
            {
                return;
            }

            var rect = _rectTransform.rect;
            var pivot = _rectTransform.pivot;

            var xFromLeft = localPoint.x + rect.width * pivot.x;
            var yFromTop = rect.height * (1f - pivot.y) - localPoint.y;

            var widthCells = Mathf.Max(1, _itemView.Config.sizeInCellsX);
            var heightCells = Mathf.Max(1, _itemView.Config.sizeInCellsY);

            var pitchX = widthCells > 0 ? rect.width / widthCells : rect.width;
            var pitchY = heightCells > 0 ? rect.height / heightCells : rect.height;
            pitchX = Mathf.Max(1e-3f, pitchX);
            pitchY = Mathf.Max(1e-3f, pitchY);

            _grabbedCellOffsetX = Mathf.Clamp(Mathf.FloorToInt(xFromLeft / pitchX), 0, widthCells - 1);
            _grabbedCellOffsetY = Mathf.Clamp(Mathf.FloorToInt(yFromTop / pitchY), 0, heightCells - 1);
        }
    }
}
