using UnityEngine;

namespace DevTools
{
    public static class DevGUIUtility
    {
        private enum DragState { None, Scroll, GUI }

        private const float DragThreshold = 10;

        private static DragState _dragState;
        private static Vector2 _dragDelta;
        private static int _dragControl;

        private static void HandleMouseDrag(int controlId, Vector2 delta, ref Vector2 scroll)
        {
            if (_dragState == DragState.GUI)
                return;
            if (_dragState == DragState.Scroll && _dragControl == controlId)
            {
                scroll.y += delta.y;
                return;
            }
            if (_dragControl == controlId)
            {
                _dragState = DragState.Scroll;
                return;
            }

            if (_dragControl != 0)
                return;

            _dragDelta += delta;
            if (Mathf.Abs(_dragDelta.x) > DragThreshold)
            {
                _dragState = DragState.GUI;
                return;
            }
            if (Mathf.Abs(_dragDelta.y) > DragThreshold)
            {
                _dragState = DragState.Scroll;
                _dragControl = controlId;
                GUIUtility.hotControl = controlId;
            }
            Event.current.Use();
        }

        public static void HandleDragScroll(ref Vector2 scroll)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (GUIUtility.hotControl == 0)
                        GUIUtility.hotControl = id;
                    break;
                case EventType.MouseDrag:
                    HandleMouseDrag(id, e.delta, ref scroll);
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                        GUIUtility.hotControl = 0;

                    _dragDelta = Vector2.zero;
                    _dragState = DragState.None;
                    _dragControl = 0;
                    break;
            }
        }

        public static Rect GetSquareRect(Rect rect, float padding = 0)
        {
            var square = rect;
            square.height = rect.height - padding * 2;
            square.width = square.height;
            square.x += padding;
            square.y += padding;
            return square;
        }
    }
}
