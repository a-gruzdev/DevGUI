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
            var height = Styles.DropDownItem.CalcHeight(GUIContent.none, 1);
            var margin = Styles.DropDownItem.margin;
            var spacing = Mathf.Max(margin.top, margin.bottom);
            height += spacing;
            var itemCount = Mathf.Min(MaxItems, _enumData.Names.Length);
            _rect.height = height * itemCount;
            _rect.height += Styles.Panel.padding.vertical + spacing + 2;

            if (_rect.yMax > DevGUI.ScreenRect.yMax)
                _rect.y -= _rect.height + rect.height;
        }

        private bool DoItem(string text, bool isChecked)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDownItem);
            var pressed = GUI.Button(rect, text, Styles.DropDownItem);
            if (Event.current.type == EventType.Repaint)
            {
                Styles.CheckMark.Draw(DevGUIUtility.GetSquareRect(rect, 4), false, false, isChecked, false);
            }
            return pressed;
        }

        public override void PopupGUI()
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
