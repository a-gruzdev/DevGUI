using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace DevTools
{
    public static class Styles
    {
        public static GUIStyle Foldout;
        public static GUIStyle SideButton;
        public static GUIStyle SliderLabel;
        public static GUIStyle Title;
        public static GUIStyle Panel;
        public static GUIStyle DropDown;
        public static GUIStyle DropDownItem;
        public static GUIStyle CheckMark;
        public static GUIStyle CircleCursor;
        public static GUIStyle RectCursor;
        public static GUIStyle HueSlider;
        public static GUIStyle Checker;

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
            CircleCursor = skin.GetStyle("circlecursor");
            RectCursor = skin.GetStyle("rectcursor");
            HueSlider = skin.GetStyle("hueslider");
            Checker = skin.GetStyle("checker");
        }
    }

    public class DevGUI : MonoBehaviour
    {
        private const float Resolution = 800;
        private const float PanelWidth = 300;
        private static readonly GUILayoutOption TitleWidth = GUILayout.Width(100);
        private static readonly GUILayoutOption VectorLabelWidth = GUILayout.Width(10);
        private static readonly string[] VectorLabels = { "x", "y", "z", "w" };

        private static readonly Dictionary<string, List<Action>> _categories = new();
        private static readonly Dictionary<string, bool> _foldouts = new();
        private static DevGUI _instance;
        private static Popup _popup;

        private readonly GUIContent _tempContent = new();
        private static readonly GUIContent _dropdownContent = new();

        private static float _guiScale;
        private static Rect _window;
        private static Rect _screen;
        private bool _hidden = true;
        private Vector2 _scroll;

        public static Rect ScreenRect => _screen;

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
            ColorPicker.Init();
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
            _window = new Rect(0, 0, PanelWidth, _screen.height);
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

            if (GUILayout.Button(GetArrow(!RightSide), GUILayout.Width(25)))
                RightSide = !RightSide;

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

        private static void GUITitle(string title) => GUILayout.Label(title, Styles.Title, TitleWidth);

        public static float Slider(string title, float value, float min, float max)
        {
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{value:0.000}", Styles.SliderLabel);
            value = GUILayout.HorizontalSlider(value, min, max);
            var rect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();
            GUI.Label(rect, title, Styles.Title);
            return value;
        }

        public static string TextField(string title, string text)
        {
            using (new TitleScope(title))
                return GUILayout.TextField(text);
        }

        public static int IntField(string title, int value)
        {
            using (new TitleScope(title))
                return IntField(value);
        }

        public static int IntField(int value)
        {
            var raw = DevGUIUtility.NumericField(value, out var edited);
            if (edited && int.TryParse(raw, out var parsed))
                return parsed;
            return value;
        }

        public static float FloatField(string title, float value)
        {
            using (new TitleScope(title))
                return FloatField(value);
        }

        public static float FloatField(float value)
        {
            var raw = DevGUIUtility.NumericField(value, out var edited);
            if (edited && float.TryParse(raw, out var parsed))
                return parsed;
            return value;
        }

        private static unsafe T VectorField<T>(string title, T value) where T : struct
        {
            var componentsCount = UnsafeUtility.SizeOf<T>() / sizeof(float);
            if (componentsCount > VectorLabels.Length)
                throw new IndexOutOfRangeException("Vector can have up to 4 components");

            float* ptr = (float*)UnsafeUtility.AddressOf(ref value);
            using (new TitleScope(title))
            {
                for (int i = 0; i < componentsCount; i++)
                {
                    GUILayout.Label(VectorLabels[i], VectorLabelWidth);
                    ptr[i] = FloatField(ptr[i]);
                }
            }
            return value;
        }

        private static unsafe T VectorIntField<T>(string title, T value) where T : struct
        {
            var componentsCount = UnsafeUtility.SizeOf<T>() / sizeof(int);
            if (componentsCount > 3)
                throw new IndexOutOfRangeException("VectorInt can have up to 3 components");

            int* ptr = (int*)UnsafeUtility.AddressOf(ref value);
            using (new TitleScope(title))
            {
                for (int i = 0; i < componentsCount; i++)
                {
                    GUILayout.Label(VectorLabels[i], VectorLabelWidth);
                    ptr[i] = IntField(ptr[i]);
                }
            }
            return value;
        }

        public static Vector2 Vector2Field(string title, Vector2 value) => VectorField(title, value);
        public static Vector3 Vector3Field(string title, Vector3 value) => VectorField(title, value);
        public static Vector4 Vector4Field(string title, Vector4 value) => VectorField(title, value);

        public static Vector2Int Vector2IntField(string title, Vector2Int value) => VectorIntField(title, value);
        public static Vector3Int Vector3IntField(string title, Vector3Int value) => VectorIntField(title, value);

        public static Color ColorField(string title, Color value)
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            using (new TitleScope(title))
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, GUI.skin.button);
                if (GUI.Button(rect, GUIContent.none))
                    ColorPicker.Show(id, value);

                ColorPicker.DrawColorRect(rect, value);
            }
            if (ColorPicker.TryGetColor(id, out var color))
                return color;
            return value;
        }

        public static T EnumField<T>(string title, T value) where T : Enum
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            using (new TitleScope(title))
            {
                var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDown);
                _dropdownContent.text = EnumUtility.GetName(value);
                if (GUI.Button(rect, _dropdownContent, Styles.DropDown))
                {
                    var popupRect = rect;
                    popupRect.y += popupRect.height;
                    popupRect.position /= _guiScale;
                    popupRect = GUIUtility.GUIToScreenRect(popupRect);
                    EnumPopup.Show(id, popupRect, value);
                }
            }

            if (EnumPopup.TryGetValue(id, out var selected))
            {
                GUI.changed = true;
                return (T)Enum.ToObject(typeof(T), selected);
            }
            return value;
        }

        public static Vector2 GUIToScreenPoint(Vector2 pos) => GUIUtility.GUIToScreenPoint(pos / _guiScale);

        internal readonly ref struct TitleScope
        {
            public TitleScope(string text)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUITitle(text);
            }

            public void Dispose() => GUILayout.EndHorizontal();
        }

        public abstract class Popup
        {
            public int Id { get; protected set; }
            protected Rect _rect;

            public Popup(Rect rect)
            {
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
                _rect = GUILayout.Window(Id, _rect, OnWindow, GUIContent.none, Styles.Panel);
            }

            private void OnWindow(int id) => PopupGUI();

            public void Close()
            {
                Id = 0;
                _popup = null;
            }

            public void Show(int id)
            {
                Id = id;
                _popup = this;
            }
        }
    }
}
