using System;
using UnityEngine;

namespace DevTools
{
    internal class EnumPopup : DevGUI.Popup
    {
        private const int MaxItems = 4;
        private readonly EnumData _enumData;
        private Vector2 _scroll;

        private static bool _dirty;
        private static int _value;
        private static int _lastId;

        private readonly Action guiMode;

        private EnumPopup(Rect rect, int value, EnumData enumData) : base(rect)
        {
            _value = value;
            _enumData = enumData;
            guiMode = _enumData.IsFlags ? FlagsGUI : EnumGUI;
            _rect.height = CalculateHeight(_enumData.Names.Length);

            if (_rect.yMax > DevGUI.ScreenRect.yMax)
                _rect.y -= _rect.height + rect.height;
        }

        private static float CalculateHeight(int count)
        {
            var height = Styles.DropDownItem.CalcHeight(GUIContent.none, 1);
            var margin = Styles.DropDownItem.margin;
            var spacing = Mathf.Max(margin.top, margin.bottom);
            height += spacing;
            height *= Mathf.Min(MaxItems, count);
            height += Styles.Panel.padding.vertical + spacing + 3;
            return height;
        }

        private bool DoItem(string text, bool isChecked)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDownItem);
            var pressed = GUI.Button(rect, text, Styles.DropDownItem);
            if (Event.current.type == EventType.Repaint)
            {
                var markRect = DevGUIUtility.GetSquareRectHorizontal(rect, 0, 8);
                Styles.CheckMark.Draw(markRect, false, false, isChecked, false);
            }
            return pressed;
        }

        protected override void PopupGUI()
        {
            var dragId = GUIUtility.GetControlID(FocusType.Passive);
            DevGUIUtility.HandleDragScroll(dragId, ref _scroll);
            _scroll = GUILayout.BeginScrollView(_scroll);
            guiMode();
            GUILayout.EndScrollView();
        }

        private void EnumGUI()
        {
            for (int i = 0; i < _enumData.Names.Length; i++)
            {
                var value = _enumData.Values[i];
                if (DoItem(_enumData.Names[i], value == _value))
                {
                    _value = value;
                    _lastId = Id;
                    _dirty = true;
                    Close();
                }
            }
        }

        private void FlagsGUI()
        {
            for (int i = 0; i < _enumData.Names.Length; i++)
            {
                var flag = _enumData.Values[i];
                if (DoItem(_enumData.Names[i], HasFlag(flag)))
                {
                    if (i < 2) // none or all flags
                        _value = flag;
                    else
                        _value ^= flag;
                    _lastId = Id;
                    if (_value > _enumData.AllValuesMask)
                        Cleanup();
                    _dirty = true;
                }
            }
        }

        // basically reset "trailing" bits to zero
        private void Cleanup()
        {
            var mask = 0;
            foreach (var value in _enumData.Values)
            {
                if (HasFlag(value))
                    mask |= value;
            }
            _value = mask;
        }

        private bool HasFlag(int flag)
        {
            if (flag != 0)
                return (_value & flag) == flag;
            return flag == _value;
        }

        public static bool TryGetValue(int id, out int value)
        {
            value = 0;
            if (_lastId != id || !_dirty)
                return false;

            _dirty = false;
            value = _value;
            return true;
        }

        public static void Show<T>(int id, Rect rect, T value) where T : Enum
        {
            var enumType = typeof(T);
            new EnumPopup(rect, value.GetHashCode(), EnumUtility.GetData(enumType)).Show(id);
        }
    }
}
