using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace DevTools
{
    public static class Styles
    {
        public static GUIStyle Foldout;
        public static GUIStyle ArrowUp;
        public static GUIStyle ArrowDown;
        public static GUIStyle ArrowRight;
        public static GUIStyle ArrowLeft;
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
            ArrowUp = skin.GetStyle("arrowup");
            ArrowDown = skin.GetStyle("arrowdown");
            ArrowRight = skin.GetStyle("arrowright");
            ArrowLeft = skin.GetStyle("arrowleft");
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

    [Icon("SceneViewTools@2x")]
    [AddComponentMenu("Tools/DevGUI")]
    public class DevGUI : MonoBehaviour
    {
        private const float IndentWidth = 8;
        private static readonly GUILayoutOption TitleWidth = GUILayout.Width(100);
        private static readonly GUILayoutOption VectorLabelWidth = GUILayout.Width(10);
        private static readonly GUILayoutOption IconButtonWidth = GUILayout.Width(25);
        private static readonly string[] VectorLabels = { "x", "y", "z", "w" };

        private static readonly GUIFolder _rootFolder = new("Root");
        private static readonly List<GUIFolder> _foldersBuffer = new();
        private static readonly List<(string path, Action action, int order)> _addedGUIs = new();
        private static readonly List<(string path, Action action)> _removedGUIs = new();

        private static readonly List<Popup> _popups = new();
        private static bool _isMouseDownEvent;

        private static float _guiScale;
        private static Rect _window;
        private static Rect _screen;
        private bool _hidden = true;
        private Vector2 _scroll;
        private GUISkin _skin;

        public float Resolution = 800;
        public float PanelWidth = 300;
        public bool RightSide = true;

        public static Rect ScreenRect => _screen;
        public static DevGUI Instance { get; private set; }

        private static bool IsOpen => Instance != null && !Instance._hidden;

        private void Awake()
        {
            if (Instance != null)
            {
                Debug.LogWarning("[DevGUI] Only one instance is allowed", gameObject);
                Destroy(this);
                return;
            }
            _skin = Resources.Load<GUISkin>("DevGUISkin");
            Debug.Assert(_skin != null, "[DevGUI] DevGUISkin not found");
            Styles.Init(_skin);
            ColorPicker.Init();
            Instance = this;
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

        private static void FolderGUI(GUIFolder folder, int indent, bool foldable = true)
        {
            if (foldable)
            {
                folder.Unfold = Foldout(folder.Unfold, folder.Name);
                if (!folder.Unfold)
                    return;
            }
            folder.OnGUI();

            using var indentScope = new IndentScope(indent * IndentWidth);
            foreach (var child in folder.Folders)
                FolderGUI(child, indent + 1);
        }

        private static void DrawSquare(Rect rect, float anchor, float padding, GUIStyle style)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            style.Draw(DevGUIUtility.GetSquareRectHorizontal(rect, anchor, padding), false, false, false, false);
        }

        private GUIStyle GetArrow(bool right) => right ? Styles.ArrowRight : Styles.ArrowLeft;

        // workaround for closing popups on outside tap
        // EventType.MouseDown is replaced with EventType.Used in some cases by window gui
        // so it's recorded here before it was used
        private static void CheckMouseDownEvent()
        {
            if (_isMouseDownEvent == true)
                return;
            _isMouseDownEvent = Event.current.type == EventType.MouseDown;
        }

        private void PopupsGUI()
        {
            CheckMouseDownEvent();
            var checkClose = true;
            for (int i = _popups.Count - 1; i >= 0; i--)
                checkClose = !_popups[i].OnGUI(checkClose);

            _isMouseDownEvent = false;
        }

        private void HideButtonToggle()
        {
            const float Width = 28;
            const float Height = 50;
            const float TopOffset = 30;
            var btnRect = new Rect(_window.x - Width, TopOffset, Width, Height);
            if (!RightSide)
                btnRect.x += _window.width + Width;

            _hidden = GUI.Toggle(btnRect, _hidden, GUIContent.none, Styles.SideButton);

            if (Event.current.type == EventType.Repaint)
            {
                var arrowRect = DevGUIUtility.GetSquareRectVertical(btnRect, 0.5f, 8);
                GetArrow(_hidden ^ RightSide).Draw(arrowRect, false, false, false, false);
            }
        }

        private static bool IconButton(GUIStyle iconStyle)
        {
            var pressed = GUILayout.Button(GUIContent.none, IconButtonWidth);
            DrawSquare(GUILayoutUtility.GetLastRect(), 0.5f, 8, iconStyle);
            return pressed;
        }

        private void TitleBarGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Developer Tools", GUILayout.ExpandWidth(false));

            if (IconButton(GetArrow(!RightSide)))
                RightSide = !RightSide;

            if (_scroll.y > 20)
            {
                if (IconButton(Styles.ArrowUp))
                    _scroll.y = 0;
            }
            GUILayout.EndHorizontal();
        }

        private void MainGUI()
        {
            _window = new Rect(0, 0, PanelWidth, _screen.height);
            PopupsGUI();

            if (RightSide)
                SnapToRight(ref _window, _screen);
            if (_hidden)
                _window.x += RightSide ? _window.width : -_window.width;

            HideButtonToggle();
            if (_hidden)
                return;

            var dragId = GUIUtility.GetControlID(FocusType.Passive);
            if (_window.Contains(Event.current.mousePosition))
                DevGUIUtility.HandleDragScroll(dragId, ref _scroll);

            GUILayout.BeginArea(_window, Styles.Panel);
            TitleBarGUI();
            _scroll = GUILayout.BeginScrollView(_scroll);
            FolderGUI(_rootFolder, 0, false);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static void CleanupGUI()
        {
            if (_removedGUIs.Count > 0)
            {
                foreach (var (path, action) in _removedGUIs)
                    RemoveGUIImmediate(path, action);

                _removedGUIs.Clear();
            }
            if (_addedGUIs.Count > 0)
            {
                foreach (var (path, action, order) in _addedGUIs)
                    AddGUIImmediate(path, action, order);

                _addedGUIs.Clear();
            }
        }

        private void OnGUI()
        {
            CleanupGUI();
            GUI.skin = _skin;
            _guiScale = Screen.width / Resolution;
            var guiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * _guiScale);
            _screen.size = new Vector2(Screen.width / _guiScale, Screen.height / _guiScale);
            MainGUI();
            GUI.matrix = guiMatrix;
        }

        public static void AddGUI(string category, Action guiFunc, int sortingOrder = 0)
        {
            if (IsOpen)
                _addedGUIs.Add((category, guiFunc, sortingOrder));
            else
                AddGUIImmediate(category, guiFunc, sortingOrder);
        }

        private static void AddGUIImmediate(string category, Action guiFunc, int sortingOrder = 0)
        {
            _rootFolder.GetAtPath(category).AddGUI(guiFunc, sortingOrder);
        }

        public static void RemoveGUI(string category, Action guiFunc)
        {
            if (IsOpen)
                _removedGUIs.Add((category, guiFunc));
            else
                RemoveGUIImmediate(category, guiFunc);
        }

        private static void RemoveGUIImmediate(string category, Action guiFunc)
        {
            if (!_rootFolder.FindAtPath(category, _foldersBuffer))
                return;

            _foldersBuffer[^1].RemoveGUI(guiFunc);
            for (int i = _foldersBuffer.Count - 1; i >= 0; i--)
                _foldersBuffer[i].RemoveEmptyFolders();
        }

        public static bool Foldout(bool value, string title)
        {
            value = GUILayout.Toggle(value, title, Styles.Foldout);
            DrawSquare(GUILayoutUtility.GetLastRect(), 0, 5, value ? Styles.ArrowDown : Styles.ArrowRight);
            return value;
        }

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
            {
                GUI.changed = true;
                return color;
            }
            return value;
        }

        public static T EnumField<T>(string title, T value) where T : Enum
        {
            using (new TitleScope(title))
                return EnumField(value);
        }

        public static T EnumField<T>(T value) where T : Enum
        {
            var id = GUIUtility.GetControlID(FocusType.Passive);
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.DropDown);
            if (GUI.Button(rect, EnumUtility.GetName(value), Styles.DropDown))
            {
                var popupRect = rect;
                popupRect.y += popupRect.height;
                popupRect.position /= _guiScale;
                popupRect = GUIUtility.GUIToScreenRect(popupRect);
                EnumPopup.Show(id, popupRect, value);
            }
            DrawSquare(rect, 1, 8, Styles.ArrowDown);

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
                GUILayout.Label(text, Styles.Title, TitleWidth);
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

            protected abstract void PopupGUI();

            public bool OnGUI(bool checkClose)
            {
                if (checkClose)
                {
                    var e = Event.current;
                    if (_isMouseDownEvent)
                    {
                        if (!_rect.Contains(e.mousePosition))
                        {
                            Close();
                            return false;
                        }
                        else if (e.type == EventType.MouseDown)
                            e.Use();
                    }
                }
                _rect = GUILayout.Window(Id, _rect, OnWindow, GUIContent.none, Styles.Panel);
                return true;
            }

            private void OnWindow(int id)
            {
                CheckMouseDownEvent();
                PopupGUI();
            }

            public void Close()
            {
                Id = 0;
                _popups.Remove(this);
            }

            public void Show(int id)
            {
                Id = id;
                GUI.BringWindowToFront(id);
                _popups.Add(this);
            }
        }
    }
}
