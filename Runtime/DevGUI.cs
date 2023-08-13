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

            internal static void Init(GUISkin skin)
            {
                Foldout = skin.GetStyle("foldout");
                SideButton = skin.GetStyle("sidebutton");
                SliderLabel = skin.GetStyle("sliderlabel");
                Title = skin.GetStyle("title");
                Panel = skin.GetStyle("panel");
            }
        }

        private enum DragState { None, Scroll, GUI }

        private const float Resolution = 800;
        private const float DragThreshold = 10;

        private static readonly Dictionary<string, List<Action>> _categories = new();
        private static readonly Dictionary<string, bool> _foldouts = new();
        private static DevGUI _instance;

        private readonly GUIContent _tempContent = new();

        private static float _guiScale;
        private static Rect _window;
        private static Rect _screen;
        private bool _hidden = true;
        private Vector2 _scroll;
        private DragState _dragState;
        private Vector2 _dragDelta;

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

        private void HandleMouseDrag(Vector2 delta)
        {
            if (_dragState == DragState.GUI)
                return;
            if (_dragState == DragState.Scroll)
            {
                _scroll.y += delta.y;
                return;
            }
            if (GUIUtility.hotControl == 0)
            {
                _dragState = DragState.Scroll;
                return;
            }

            _dragDelta += delta;
            if (Mathf.Abs(_dragDelta.x) > DragThreshold)
            {
                _dragState = DragState.GUI;
                return;
            }
            if (Mathf.Abs(_dragDelta.y) > DragThreshold)
            {
                _dragState = DragState.Scroll;
                GUIUtility.hotControl = 0;
            }
        }

        private void HandleInput()
        {
            var e = Event.current;
            if (!_window.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    HandleMouseDrag(e.delta);
                    break;
                case EventType.MouseUp:
                    _dragDelta = Vector2.zero;
                    _dragState = DragState.None;
                    break;
            }
        }

        private void PanelGUI()
        {
            foreach (var (title, guiList) in _categories)
            {
                _foldouts[title] = Foldout(_foldouts[title], title);
                if (!_foldouts[title])
                    continue;
                foreach (var gui in guiList)
                {
                    GUILayout.BeginVertical(Skin.box);
                    gui();
                    GUILayout.EndVertical();
                }
            }
        }

        private Texture2D GetArrow(bool right) => right ? TexArrowRight : TexArrowLeft;

        private void OnGUI()
        {
            GUI.skin = Skin;
            _guiScale = Screen.width / Resolution;
            var guiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * _guiScale);
            _screen.size = new Vector2(Screen.width / _guiScale, Screen.height / _guiScale);

            _window = new Rect(0, 0, 300, _screen.height);
            // _popup?.OnGUI();

            if (RightSide)
                SnapToRight(ref _window, _screen);
            if (_hidden)
                _window.x += RightSide ? _window.width : -_window.width;

            const float SideButtonWidth = 30;
            var btnRect = new Rect(_window.x - SideButtonWidth, 0, SideButtonWidth, 50);
            if (!RightSide)
                btnRect.x += _window.width + SideButtonWidth;

            _hidden = GUI.Toggle(btnRect, _hidden, GetArrow(_hidden ^ RightSide), Styles.SideButton);
            if (_hidden)
                return;

            HandleInput();

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

            GUI.matrix = guiMatrix;
        }

        private bool Foldout(bool value, string title)
        {
            _tempContent.image = value ? TexArrowDown : TexArrowRight;
            _tempContent.text = title;
            return GUILayout.Toggle(value, _tempContent, Styles.Foldout);
        }

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

        // private static int fieldId;
        // private static string tmpStr;

        // public static float FloatField(string title, float value)
        // {
        //     fieldId = GUIUtility.GetControlID(FocusType.Keyboard) + 1;
        //     if (GUIUtility.keyboardControl != fieldId)
        //         tmpStr = value.ToString();
        //     tmpStr = TextField(title, tmpStr);
        //     GUILayout.Label($"focus: {fieldId == GUIUtility.keyboardControl}");
        //     if (float.TryParse(tmpStr, out var v))
        //         return v;
        //     return value;
        // }

        // private static Popup _popup;

        // public static int EnumField<T>(T value) where T : Enum
        // {
        //     if (GUILayout.Button(value.ToString()))
        //     {
        //         var pos = Event.current.mousePosition;
        //         Debug.Log(pos);
        //         _popup = new EnumPopup(pos, Enum.GetNames(typeof(T)));
        //     }
        //     return 0;
        // }

        // private abstract class Popup
        // {
        //     public Rect rect;

        //     public void OnGUI()
        //     {
        //         var id = GUIUtility.GetControlID(FocusType.Passive);
        //         GUI.Window(id, rect, OnWindow, "NULL", GUI.skin.box);
        //     }

        //     public void OnWindow(int id)
        //     {
        //         var e = Event.current;
        //         switch (e.type)
        //         {
        //             case EventType.MouseDown:
        //                 if (!rect.Contains(e.mousePosition))
        //                     Close();
        //                 break;
        //         }
        //         PopupGUI();
        //     }

        //     public abstract void PopupGUI();

        //     public void Close()
        //     {
        //         _popup = null;
        //     }
        // }

        // private class EnumPopup : Popup
        // {
        //     private string[] _values;

        //     public EnumPopup(Vector2 pos, string[] values)
        //     {
        //         _values = values;
        //         rect = new Rect(pos, new Vector2(80, 150));
        //     }

        //     public override void PopupGUI()
        //     {
        //         GUILayout.BeginArea(rect);
        //         GUILayout.BeginVertical("Box");

        //         for (int i = 0; i < _values.Length; i++)
        //             GUILayout.Button(_values[i]);

        //         GUILayout.EndVertical();
        //         GUILayout.EndArea();
        //     }
        // }
    }
}
