using System;
using System.Collections.Generic;
using UnityEngine;

namespace DevTools
{
    public class DevGUI : MonoBehaviour
    {
        private class GUITexture
        {
            public Texture2D Texture { get; private set; }
            private readonly Color[] m_data = new Color[1];

            public float Color
            {
                get => m_data[0].r;
                set
                {
                    m_data[0].r = value;
                    m_data[0].g = value;
                    m_data[0].b = value;
                    Refresh();
                }
            }
            public float Alpha
            {
                get => m_data[0].a;
                set
                {
                    m_data[0].a = value;
                    Refresh();
                }
            }

            public GUITexture(float color, float alpha)
            {
                Texture = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                Color = color;
                Alpha = alpha;
            }

            private void Refresh()
            {
                Texture.SetPixels(m_data);
                Texture.Apply();
            }

            public void Sliders(string name)
            {
                Color = Slider(name, Color, 0, 1);
                Alpha = Slider($"{name}.a", Alpha, 0, 1);
            }

            public void Release() => Destroy(Texture);
        }

        private enum DragState { None, Scroll, GUI }

        private const float k_Resolution = 800;
        private const float k_DragThreshold = 10;

        private static readonly Dictionary<string, List<Action>> s_Categories = new();
        private static readonly Dictionary<string, bool> s_Foldouts = new();

        private Rect m_screen;
        private Rect m_window;
        private bool m_hidden;
        private Vector2 m_scroll;
        private DragState m_dragState;
        private Vector2 m_dragDelta;

        private GUITexture backgroundTex;
        private GUITexture buttonTex;
        private GUITexture buttonPressedTex;
        private GUITexture boxTex;
        private GUITexture sliderBackTex;

        public GUISkin skin;

        private void Awake()
        {
            backgroundTex = new GUITexture(0.03f, 0.8f);
            buttonTex = new GUITexture(0.7f, 0.5f);
            buttonPressedTex = new GUITexture(0.5f, 0.5f);
            boxTex = new GUITexture(0.2f, 0.8f);
            sliderBackTex = new GUITexture(0.2f, 0.8f);
        }

        private void OnDestroy()
        {
            backgroundTex.Release();
            buttonTex.Release();
            buttonPressedTex.Release();
            boxTex.Release();
            sliderBackTex.Release();
        }

        private void Start()
        {
            skin.GetStyle("foldout").normal.background = boxTex.Texture;
            skin.GetStyle("sidebutton").normal.background = backgroundTex.Texture;

            skin.button.normal.background = buttonTex.Texture;
            skin.button.active.background = buttonPressedTex.Texture;
            skin.box.normal.background = backgroundTex.Texture;

            skin.horizontalSlider.normal.background = backgroundTex.Texture;
            skin.horizontalSliderThumb.normal.background = buttonTex.Texture;

            skin.verticalScrollbar.normal.background = backgroundTex.Texture;
            skin.verticalScrollbarThumb.normal.background = buttonTex.Texture;

            skin.horizontalSlider.normal.background = sliderBackTex.Texture;

        }

        private void OnEnable()
        {
            // AddGUI("Example", ExampleGUI);
            AddGUI("Settings", SettingsGUI);
            AddGUI("Device Info", InfoGUI);
        }

        private void OnDisable()
        {
            // RemoveGUI("Example", ExampleGUI);
            RemoveGUI("Settings", SettingsGUI);
            RemoveGUI("Device Info", InfoGUI);
        }

        private GUIContent buttonContent = new("Button");

        private void ExampleGUI()
        {
            var r = GUILayoutUtility.GetRect(buttonContent, skin.button);
            if (Event.current.type == EventType.Repaint)
                skin.button.Draw(r, buttonContent, false, false, false, false);

            r = GUILayoutUtility.GetRect(buttonContent, skin.button);
            if (Event.current.type == EventType.Repaint)
                skin.button.Draw(r, buttonContent, false, true, false, false);

            text = TextField("Text Field", text);
            f = FloatField("float Field", f);
            f2 = FloatField("Speed", f2);
        }

        float f = 4.2f;
        float f2 = 8.9f;
        string text = "lol";
        public static float TitleWidth = 100;
        bool test;

        private void SettingsGUI()
        {
            boxTex.Sliders(nameof(boxTex));
            backgroundTex.Sliders(nameof(backgroundTex));
            buttonTex.Sliders(nameof(buttonTex));
            sliderBackTex.Sliders(nameof(sliderBackTex));
        }

        private void InfoGUI()
        {
            GUILayout.Label($"Device: {SystemInfo.deviceModel}");
            GUILayout.Label($"Resolution: {Screen.currentResolution}");
            GUILayout.Label($"Graphics API: {SystemInfo.graphicsDeviceType}");
            GUILayout.Label($"Graphics Device: {SystemInfo.graphicsDeviceName}");
        }

        private static void SnapToRight(ref Rect rect, Rect target) => rect.x = target.xMax - rect.width;
        private static void SnapToBottom(ref Rect rect, Rect target) => rect.y = target.yMax - rect.height;

        public static void AddGUI(string category, Action guiFunc)
        {
            if (!s_Categories.TryGetValue(category, out var guiList))
            {
                guiList = new List<Action>();
                s_Categories[category] = guiList;
                s_Foldouts[category] = false;
            }
            guiList.Add(guiFunc);
        }

        public static void RemoveGUI(string category, Action guiFunc)
        {
            if (!s_Categories.TryGetValue(category, out var guiList))
                return;
            guiList.Remove(guiFunc);
            if (guiList.Count == 0)
                s_Categories.Remove(category);
        }

        private void HandleMouseDrag(Vector2 delta)
        {
            if (m_dragState == DragState.GUI)
                return;
            if (m_dragState == DragState.Scroll)
            {
                m_scroll.y += delta.y;
                return;
            }
            if (GUIUtility.hotControl == 0)
            {
                m_dragState = DragState.Scroll;
                return;
            }

            m_dragDelta += delta;
            if (Mathf.Abs(m_dragDelta.x) > k_DragThreshold)
            {
                m_dragState = DragState.GUI;
                return;
            }
            if (Mathf.Abs(m_dragDelta.y) > k_DragThreshold)
            {
                m_dragState = DragState.Scroll;
                GUIUtility.hotControl = 0;
            }
        }

        private void HandleInput()
        {
            var e = Event.current;
            if (!m_window.Contains(e.mousePosition))
                return;

            switch (e.type)
            {
                case EventType.MouseDrag:
                    HandleMouseDrag(e.delta);
                    break;
                case EventType.MouseUp:
                    m_dragDelta = Vector2.zero;
                    m_dragState = DragState.None;
                    break;
            }
        }

        public static float Slider(string title, float value, float min, float max)
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label($"{value:0.000}", "sliderlabel");
            value = GUILayout.HorizontalSlider(value, min, max);
            var rect = GUILayoutUtility.GetLastRect();
            GUILayout.EndHorizontal();
            GUI.Label(rect, title, "title");
            return value;
        }

        public static string TextField(string title, string text)
        {
            GUILayout.BeginHorizontal("Box");
            GUILayout.Label(title, "title", GUILayout.Width(TitleWidth));
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

        private void PanelGUI()
        {
            foreach (var (title, guiList) in s_Categories)
            {
                s_Foldouts[title] = Foldout(s_Foldouts[title], title);
                if (!s_Foldouts[title])
                    continue;
                foreach (var gui in guiList)
                {
                    GUILayout.BeginVertical(skin.box);
                    gui();
                    GUILayout.EndVertical();
                }
            }
        }

        private static bool Foldout(bool value, string title)
        {
            var icon = value ? "▼" : "►";
            return GUILayout.Toggle(value, $"{icon} {title}", "foldout");
        }

        private bool RightSide = true;

        private static string GetArrow(bool right) => right ? "▶︎" : "◀︎";

        private void OnGUI()
        {
            GUI.skin = skin;
            var scale = Screen.width / k_Resolution;
            var guiMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            m_screen.size = new Vector2(Screen.width / scale, Screen.height / scale);

            m_window = new Rect(0, 0, 300, m_screen.height);
            if (RightSide)
                SnapToRight(ref m_window, m_screen);
            if (m_hidden)
                m_window.x += RightSide ? m_window.width : -m_window.width;

            const float SideButtonWidth = 30;
            var btnRect = new Rect(m_window.x - SideButtonWidth, 0, SideButtonWidth, 50);
            if (!RightSide)
                btnRect.x += m_window.width + SideButtonWidth;

            m_hidden = GUI.Toggle(btnRect, m_hidden, GetArrow(m_hidden ^ RightSide), "sidebutton");
            if (m_hidden)
                return;

            HandleInput();

            GUILayout.BeginArea(m_window, skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Developer Tools", GUILayout.ExpandWidth(false));

            RightSide = GUILayout.Toggle(RightSide, GetArrow(!RightSide), skin.button, GUILayout.Width(25));

            if (m_scroll.y > 10)
            {
                if (GUILayout.Button("▲", GUILayout.Width(25)))
                    m_scroll.y = 0;
            }
            GUILayout.EndHorizontal();

            m_scroll = GUILayout.BeginScrollView(m_scroll);
            PanelGUI();

            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = guiMatrix;
        }
    }
}
