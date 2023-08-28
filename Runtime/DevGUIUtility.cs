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

        private static string _numericStr;
        private static TextEditor _activeTextEditor;

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

        public static Rect ClampPosInsideRect(Rect child, Rect parent)
        {
            child.x = Mathf.Clamp(child.x, parent.x, parent.xMax - child.width);
            child.y = Mathf.Clamp(child.y, parent.y, parent.yMax - child.height);
            return child;
        }

        private static char FilterNumericCharacter(char c)
        {
            switch (c)
            {
                case >= '0' and <= '9':
                case '.':
                case '-':
                    return c;
            }
            return '\0';
        }

        private static void HandleTextFieldTouch(int id, Rect rect, string text)
        {
            var e = Event.current;
            if (!rect.Contains(e.mousePosition))
                return;
            if (e.type != EventType.MouseDown)
                return;

            GUIUtility.keyboardControl = id;
            _numericStr = text;
            if (!TouchScreenKeyboard.isSupported || TouchScreenKeyboard.isInPlaceEditingAllowed)
                return;

            _activeTextEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), id);
            _activeTextEditor.keyboardOnScreen ??= TouchScreenKeyboard.Open(text, TouchScreenKeyboardType.NumbersAndPunctuation);
            e.Use();
        }

        private static bool CheckTouchKeyboardDone()
        {
            var keyboard = _activeTextEditor?.keyboardOnScreen;
            if (keyboard == null)
                return false;
            if (keyboard.status == TouchScreenKeyboard.Status.Visible)
                return false;

            GUIUtility.keyboardControl = 0;
            _activeTextEditor = null;
            return true;
        }

        internal static string NumericField<T>(Rect rect, T value, out bool edited)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive) + 1;
            edited = false;
            var strVal = value.ToString();
            HandleTextFieldTouch(id, rect, strVal);

            if (GUIUtility.keyboardControl == id)
            {
                var e = Event.current;
                e.character = FilterNumericCharacter(e.character);
                var input = GUI.TextField(rect, _numericStr);

                // this is just for touch keyboard as it's not triggers GUI.changed on input
                GUI.changed = input != _numericStr;
                _numericStr = input;

                edited = GUI.changed || CheckTouchKeyboardDone();
                return _numericStr;
            }
            else
                GUI.TextField(rect, strVal);
            return strVal;
        }

        internal static string NumericField<T>(T value, out bool edited)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.textField);
            return NumericField(rect, value, out edited);
        }
    }
}
