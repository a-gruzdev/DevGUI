using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevTools
{
    public static class DevGUIUtility
    {
        private enum DragState { None, Scroll, GUI }

        private const float DragThreshold = 10;

        private static DragState _dragState;
        private static Vector2 _dragDelta;
        private static Vector2 _dragPos;
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

        public static void HandleDragScroll(int controlId, ref Vector2 scroll)
        {
            var e = Event.current;
            switch (e.type)
            {
                case EventType.MouseDown:
                    if (GUIUtility.hotControl == 0)
                    {
                        _dragPos = e.mousePosition;
                        GUIUtility.hotControl = controlId;
                    }
                    break;
                case EventType.MouseDrag:
                    var delta = _dragPos - e.mousePosition;
                    _dragPos = e.mousePosition;
                    HandleMouseDrag(controlId, delta, ref scroll);
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                        GUIUtility.hotControl = 0;

                    _dragDelta = Vector2.zero;
                    _dragState = DragState.None;
                    _dragControl = 0;
                    break;
            }
        }

        public static Rect GetSquareRectHorizontal(Rect rect, float anchor, float padding = 0)
        {
            var square = rect;
            var size = rect.height - padding * 2;
            square.size = new Vector2(size, size);
            square.x = Mathf.Lerp(rect.x + padding, rect.xMax - size - padding, anchor);
            square.y += padding;
            return square;
        }

        public static Rect GetSquareRectVertical(Rect rect, float anchor, float padding = 0)
        {
            var square = rect;
            var size = rect.width - padding * 2;
            square.size = new Vector2(size, size);
            square.x += padding;
            square.y = Mathf.Lerp(rect.y + padding, rect.yMax - size - padding, anchor);
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
            var id = GUIUtility.GetControlID(FocusType.Passive);
            if (id > 0) // it seems GetControlID returns -1 if EventType is Used, this check prevents behaviour when keyboardControl == 0
                id++;

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

    internal readonly ref struct IndentScope
    {
        public IndentScope(float indent)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(indent);
            GUILayout.BeginVertical();
        }

        public void Dispose()
        {
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }

    public static class DevGUIExtensions
    {
        public static FastSplitIterator SplitFast(this string str, char separator) => new(str, separator);
    }

    public ref struct FastSplitIterator
    {
        private readonly char _separator;
        private readonly string _str;
        private int _offset;
        private int _sepIndex;
        private ReadOnlySpan<char> _current;

        public FastSplitIterator(string str, char separator)
        {
            _separator = separator;
            _str = str;
            _offset = 0;
            _sepIndex = 0;
            _current = default;
        }

        public bool MoveNext()
        {
            if (_sepIndex < 0)
                return false;

            _sepIndex = _str.IndexOf(_separator, _offset);
            if (_sepIndex < 0)
                _current = _str.AsSpan(_offset);
            else
                _current = _str.AsSpan(_offset, _sepIndex - _offset);

            _offset = _sepIndex + 1;
            return true;
        }

        public readonly ReadOnlySpan<char> Current => _current;
        public readonly FastSplitIterator GetEnumerator() => this;
    }

    internal class GUIFolder : IComparable<GUIFolder>
    {
        private class GUIItem : IComparable<GUIItem>
        {
            public int SortingOrder;
            public Action onGUI;
            public int CompareTo(GUIItem other) => SortingOrder.CompareTo(other.SortingOrder);
        }

        private readonly List<GUIItem> _guiItems = new();

        public readonly List<GUIFolder> Folders = new();
        public string Name { get; private set; }
        public bool Unfold;

        public GUIFolder(string name)
        {
            Name = name;
        }

        private bool Find(ReadOnlySpan<char> name, out GUIFolder folder)
        {
            folder = null;
            foreach (var child in Folders)
            {
                if (name.SequenceEqual(child.Name))
                {
                    folder = child;
                    return true;
                }
            }
            return false;
        }

        private GUIFolder GetFolder(ReadOnlySpan<char> name)
        {
            if (!Find(name, out var folder))
            {
                folder = new GUIFolder(name.ToString());
                Folders.Add(folder);
                Folders.Sort();
            }
            return folder;
        }

        private static bool IsStringEmpty(ReadOnlySpan<char> str)
        {
            if (str.Length < 1)
                return true;
            foreach (var c in str)
            {
                if (c != ' ')
                    return false;
            }
            return true;
        }

        public GUIFolder GetAtPath(string path)
        {
            var folder = this;
            foreach (var name in path.SplitFast('/'))
            {
                if (IsStringEmpty(name))
                    continue;
                folder = folder.GetFolder(name);
            }
            return folder;
        }

        public bool FindAtPath(string path, List<GUIFolder> list)
        {
            var folder = this;
            list.Clear();
            list.Add(folder);

            if (path.Length < 1 || path == "/")
                return true;

            var found = false;
            foreach (var name in path.SplitFast('/'))
            {
                if (IsStringEmpty(name))
                    continue;
                found = folder.Find(name, out folder);
                if (found)
                    list.Add(folder);
                else
                    return false;
            }
            return found;
        }

        public bool IsEmpty() => Folders.Count < 1 && _guiItems.Count < 1;
        public int CompareTo(GUIFolder other) => Name.CompareTo(other.Name);

        public void RemoveEmptyFolders()
        {
            for (int i = Folders.Count - 1; i >= 0; i--)
            {
                if (Folders[i].IsEmpty())
                    Folders.RemoveAt(i);
            }
        }

        public void AddGUI(Action guiFunc, int sortingOrder = 0)
        {
            _guiItems.Add(new GUIItem { onGUI = guiFunc, SortingOrder = sortingOrder });
            _guiItems.Sort();
        }

        public void RemoveGUI(Action guiFunc)
        {
            for (int i = 0; i < _guiItems.Count; i++)
            {
                if (_guiItems[i].onGUI == guiFunc)
                {
                    _guiItems.RemoveAt(i);
                    return;
                }
            }
        }

        public void OnGUI()
        {
            foreach (var gui in _guiItems)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                try
                {
                    GUI.changed = false;
                    gui.onGUI();
                }
                catch (Exception e)
                {
                    var guiColor = GUI.color;
                    GUI.color = Color.red;
                    GUILayout.Label(e.ToString());
                    GUI.color = guiColor;
                #if UNITY_EDITOR
                    throw;
                #endif
                }
                GUILayout.EndVertical();
            }
        }
    }
}
