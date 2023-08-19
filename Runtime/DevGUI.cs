using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevTools
{
    public class DevGUI : MonoBehaviour
    {
        private static class Styles
        {
            public static GUIStyle Foldout;
            public static GUIStyle SideButton;
            public static GUIStyle SliderLabel;
            public static GUIStyle Title;
            public static GUIStyle Panel;
            public static GUIStyle DropDown;
            public static GUIStyle DropDownItem;
            public static GUIStyle CheckMark;

            internal static void Init(GUISkin skin)
            {
                Foldout = skin.GetStyle("foldout");
                SideButton = skin.GetStyle("sidebutton");
                SliderLabel = skin.GetStyle("sliderlabel");
                Title = skin.GetStyle("title");
                Panel = skin.GetStyle("panel");
                DropDown = skin.GetStyle("dropdown");
                DropDownItem = skin.GetStyle("dropdownitem");
                CheckMark = skin.GetStyle("checkmark");
            }
        }

        private const float Resolution = 800;

        private static readonly Dictionary<string, List<Action>> _categories = new();
        private static readonly Dictionary<string, bool> _foldouts = new();
        private static DevGUI _instance;

        private readonly GUIContent _tempContent = new();
        private static readonly GUIContent _dropdownContent = new();

        private static float _guiScale;
        private static Rect _window;
        private static Rect _screen;
        private bool _hidden = true;
        private Vector2 _scroll;

        public static float TitleWidth = 100;

        public Texture2D TexArrowUp;
        public Texture2D TexArrowDown;
        public Texture2D TexArrowRight;
        public Texture2D TexArrowLeft;

        public GUISkin Skin;
        public bool RightSide = true;

        private void Awake()
        {
            if (_instance != null)
            {
                Debug.LogWarning("[DevGUI] Only one instance allowed", gameObject);
                Destroy(this);
                return;
            }

            Styles.Init(Skin);
            _dropdownContent.image = TexArrowDown;
            _instance = this;
        }

        private void OnEnable() => AddGUI("Device Info", InfoGUI);
        private void OnDisable() => RemoveGUI("Device Info", InfoGUI);

        private void InfoGUI()
        {
            GUILayout.Label($"Device: {SystemInfo.deviceModel}");
            GUILayout.Label($"Resolution: {Screen.currentResolution}");
            GUILayout.Label($"Graphics API: {SystemInfo.graphicsDeviceType}");
            GUILayout.Label($"Graphics Device: {SystemInfo.graphicsDeviceName}");
        }

        private static void SnapToRight(ref Rect rect, Rect target) => rect.x = target.xMax - rect.width;

        public static void AddGUI(string category, Action guiFunc)
        {
            if (!_categories.TryGetValue(category, out var guiList))
            {
                guiList = new List<Action>();
                _categories[category] = guiList;
                _foldouts[category] = false;
            }
            guiList.Add(guiFunc);
        }

        public static void RemoveGUI(string category, Action guiFunc)
        {
            if (!_categories.TryGetValue(category, out var guiList))
                return;
            guiList.Remove(guiFunc);
            if (guiList.Count == 0)
                _categories.Remove(category);
        }

        private void PanelGUI()
        {
            foreach (var (title, guiList) in _categories)
            {
                _foldouts[title] = Foldout(_foldouts[title], title);
                GUI.changed = false;
                if (!_foldouts[title])
                    continue;
                foreach (var gui in guiList)
                {
                    GUILayout.BeginVertical(Skin.box);
                    GUI.changed = false;
                    gui();
                    GUILayout.EndVertical();
                }
            }
        }

        private Texture2D GetArrow(bool right) => right ? TexArrowRight : TexArrowLeft;

        private void MainGUI()
        {
            _window = new Rect(0, 0, 300, _screen.height);
            _popup?.OnGUI();

            if (RightSide)
                SnapToRight(ref _window, _screen);
            if (_hidden)
                _window.x += RightSide ? _window.width : -_window.width;

            const float SideButtonWidth = 30;
            var btnRect = new Rect(_window.x - SideButtonWidth, 30, SideButtonWidth, 50);
            if (!RightSide)
                btnRect.x += _window.width + SideButtonWidth;

            _hidden = GUI.Toggle(btnRect, _hidden, GetArrow(_hidden ^ RightSide), Styles.SideButton);
            if (_hidden)
                return;

            if (_window.Contains(Event.current.mousePosition))
                DevGUIUtility.HandleDragScroll(ref _scroll);

            GUILayout.BeginArea(_window, Styles.Panel);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Developer Tools", GUILayout.ExpandWidth(false));

            RightSide = GUILayout.Toggle(RightSide, GetArrow(!RightSide), Skin.button, GUILayout.Width(25));

            if (_scroll.y > 10)
            {
                if (GUILayout.Button(TexArrowUp, GUILayout.Width(25)))
                    _scroll.y = 0;
            }
            GUILayout.EndHorizontal();

            _scroll = GUILayout.BeginScrollView(_scroll);
            PanelGUI();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void OnGUI()
        {
            GUI.skin = Skin;
            _guiScale = Screen.width / Resolution;
            var guiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * _guiScale);
            _screen.size = new Vector2(Screen.width / _guiScale, Screen.height / _guiScale);
            MainGUI();
            GUI.matrix = guiMatrix;
        }

        private bool Foldout(bool value, string title)
        {
            _tempContent.image = value ? TexArrowDown : TexArrowRight;
            _tempContent.text = title;
            return GUILayout.Toggle(value, _tempContent, Styles.Foldout);
        }

        private static void GUITitle(string title) => GUILayout.Label(title, Styles.Title, GUILayout.Width(TitleWidth));

        public static float Slider(string title, float value, float min, float max)
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label($"{value:0.000}", Styles.SliderLabel);
            value = GUILayout.HorizontalSlider(value, min, max);
            var rect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();
            GUI.Label(rect, title, Styles.Title);
            return value;
        }

        public static string TextField(string title, string text)
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label(title, Styles.Title, GUILayout.Width(TitleWidth));
            text = GUILayout.TextField(text);
            GUILayout.EndHorizontal();
            return text;
        }

        private static int fieldId;
        private static string tmpStr;

        public static float FloatField(string title, float value)
        {
            fieldId = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
            if (GUIUtility.keyboardControl != fieldId)
                tmpStr = value.ToString();
            tmpStr = TextField(title, tmpStr);
            GUILayout.Label($"focus: {fieldId == GUIUtility.keyboardControl}");
            if (float.TryParse(tmpStr, out var v))
                return v;
            return value;
        }

        private static Popup _popup;

        public static T EnumField<T>(string title, T value) where T : Enum
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            GUILayout.BeginHorizontal("Box");
            GUITitle(title);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDown);
            _dropdownContent.text = value.ToString();
            if (GUI.Button(rect, _dropdownContent, Styles.DropDown))
            {
                var popupRect = rect;
                popupRect.y += popupRect.height;
                popupRect.position /= _guiScale;
                popupRect = GUIUtility.GUIToScreenRect(popupRect);
                _popup = new EnumPopup(id, popupRect, value.GetHashCode(), Enum.GetNames(typeof(T)));
            }
            GUILayout.EndHorizontal();

            if (EnumPopup.TryGetValue(id, out var selected))
                return (T)Enum.ToObject(typeof(T), selected);
            return value;
        }

        private abstract class Popup
        {
            public readonly int Id;
            protected Rect _rect;

            public Popup(int id, Rect rect)
            {
                Id = id;
                _rect = rect;
            }

            public abstract void PopupGUI();

            public void OnGUI()
            {
                var e = Event.current;
                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (!_rect.Contains(e.mousePosition))
                            Close();
                        else
                            e.Use();
                        break;
                }

                GUI.Window(Id, _rect, OnWindow, GUIContent.none, Styles.Panel);
            }

            public void OnWindow(int id) => PopupGUI();
            public void Close() => _popup = null;
        }

        private class EnumPopup : Popup
        {
            private const int MaxItems = 4;
            private readonly string[] _values;
            private Vector2 _scroll;

            private static bool _dirty;
            private static int _value;
            private static int _lastId;

            public EnumPopup(int id, Rect rect, int value, string[] values) : base(id, rect)
            {
                _values = values;
                _value = value;
                var height = Styles.DropDownItem.CalcHeight(GUIContent.none, 1);
                var margin = Styles.DropDownItem.margin;
                var spacing = Mathf.Max(margin.top, margin.bottom);
                height += spacing;
                var itemCount = Mathf.Min(MaxItems, _values.Length);
                _rect.height = height * itemCount;
                _rect.height += Styles.Panel.padding.vertical + spacing + 2;

                if (_rect.yMax > _screen.yMax)
                    _rect.y -= _rect.height + rect.height;
            }

            private bool DoItem(string text, bool isChecked)
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDownItem);
                var pressed = GUI.Button(rect,text, Styles.DropDownItem);
                if (Event.current.type == EventType.Repaint)
                {
                    Styles.CheckMark.Draw(DevGUIUtility.GetSquareRect(rect, 4), false, false, isChecked, false);
                }
                return pressed;
            }

            public override void PopupGUI()
            {
                DevGUIUtility.HandleDragScroll(ref _scroll);
                _scroll = GUILayout.BeginScrollView(_scroll);
                for (int i = 0; i < _values.Length; i++)
                {
                    if (DoItem(_values[i], i == _value))
                    {
                        _value = i;
                        _lastId = Id;
                        _dirty = true;
                        Close();
                    }
                }
                GUILayout.EndScrollView();
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
        }
    }
}
