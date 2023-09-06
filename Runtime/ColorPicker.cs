using UnityEngine;

namespace DevTools
{
    public class ColorPicker : DevGUI.Popup
    {
        private static class Pass
        {
            public static int PickerArea = 0;
            public static int HueArea = 1;
            public static int Color = 2;
        }

        private enum EditMode { Compact, HSV, RGB }

        private const string ShaderName = "Hidden/DevGUI/ColorPicker";
        private const float CursorSize = 16;
        private const float PickerAreaMargin = 4;
        private static readonly int HuePropId = Shader.PropertyToID("_Hue");
        private static readonly int AspectPropId = Shader.PropertyToID("_Aspect");
        private static readonly Material _material = new(Shader.Find(ShaderName)) { hideFlags = HideFlags.HideAndDontSave };
        private static ColorPicker _instance;

        private Color _color;
        private Color _prevColor;
        private float _hue;
        private string _hex;
        private Vector2 _pickerValue;
        private Vector2 _pickerPos;
        private bool _dirty;
        private EditMode _editMode;

        private Vector2 _dragPos;

        internal static void Init()
        {
            _material.SetTexture("_Background", Styles.Checker.normal.background);
        }

        private ColorPicker(Rect rect) : base(rect) { }

        private void RefreshPicker(bool setDirty = false)
        {
            Color.RGBToHSV(_color, out _hue, out var s, out var v);
            _pickerValue = new Vector2(s, v);
            UpdateHex();
            if (setDirty)
                _dirty = true;
        }

        private void UpdateColor(bool setDirty = false)
        {
            var a = _color.a;
            _color = Color.HSVToRGB(_hue, _pickerValue.x, _pickerValue.y);
            _color.a = a;
            UpdateHex();
            if (setDirty)
                _dirty = true;
        }

        private void UpdateHex()
        {
            if (_color.a == 1f)
                _hex = $"#{ColorUtility.ToHtmlStringRGB(_color)}";
            else
                _hex = $"#{ColorUtility.ToHtmlStringRGBA(_color)}";
        }

        private static Vector2 GUIToPickerValue(Rect picker, Vector2 pos)
        {
            pos.x = Mathf.Clamp(pos.x, picker.x, picker.xMax);
            pos.y = Mathf.Clamp(pos.y, picker.y, picker.yMax);
            pos -= picker.position;
            pos /= picker.size;
            pos.y = 1 - pos.y;
            return pos;
        }

        private static Vector2 PickerToGUIPos(Rect picker, Vector2 value)
        {
            value.y = 1 - value.y;
            value *= picker.size;
            value += picker.position;
            return value;
        }

        private void HandlePickerArea(Rect rect)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (!rect.Contains(e.mousePosition))
                        break;
                    GUIUtility.hotControl = id;
                    _pickerValue = GUIToPickerValue(rect, e.mousePosition);
                    UpdateColor(true);
                    e.Use();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;
                    _pickerValue = GUIToPickerValue(rect, e.mousePosition);
                    UpdateColor(true);
                    e.Use();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
                case EventType.Repaint:
                    _material.SetFloat(HuePropId, _hue);
                    var pickerRect = new Rect(0, 0, CursorSize, CursorSize);
                    _pickerPos = PickerToGUIPos(rect, _pickerValue);
                    pickerRect.center = _pickerPos;
                    Styles.CircleCursor.Draw(pickerRect, false, false, false, false);
                    break;
            }
        }

        private void HandleDragArea(Rect rect)
        {
            var e = Event.current;
            var id = GUIUtility.GetControlID(FocusType.Passive);

            switch (e.type)
            {
                case EventType.MouseDown:
                    if (!rect.Contains(e.mousePosition))
                        break;
                    GUIUtility.hotControl = id;
                    _dragPos = DevGUI.GUIToScreenPoint(e.mousePosition);
                    e.Use();
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl != id)
                        break;
                    var screenPos = DevGUI.GUIToScreenPoint(e.mousePosition);
                    _rect.position += screenPos - _dragPos;
                    _dragPos = screenPos;
                    e.Use();
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        e.Use();
                    }
                    break;
            }
        }

        private float HueSlider(float value)
        {
            var rect = GUILayoutUtility.GetRect(GUIContent.none, Styles.HueSlider);
            DrawPass(rect, Pass.HueArea);
            return GUI.VerticalSlider(rect, value, 0f, 1f, Styles.HueSlider, Styles.RectCursor);
        }

        private void HeaderGUI()
        {
            GUILayout.Label(_hex);
            var rect = GUILayoutUtility.GetLastRect();

            const float width = 40;
            var colRect = rect;
            colRect.x += colRect.width - width;
            colRect.width = width;

            DrawColorRect(colRect, _color);
            colRect.x -= width;
            DrawColorRect(colRect, _prevColor);
            HandleDragArea(rect);
        }

        private void SlidersGUI()
        {
            _editMode = DevGUI.EnumField(_editMode);
            if (GUI.changed)
            {
                _rect.height = 0;
                GUI.changed = false;
            }

            switch (_editMode)
            {
                case EditMode.RGB:
                    _color.r = DevGUI.Slider("Red", _color.r, 0, 1);
                    _color.g = DevGUI.Slider("Green", _color.g, 0, 1);
                    _color.b = DevGUI.Slider("Blue", _color.b, 0, 1);
                    if (GUI.changed)
                    {
                        RefreshPicker(true);
                        GUI.changed = false;
                    }
                    break;
                case EditMode.HSV:
                    _hue = DevGUI.Slider("Hue", _hue, 0, 1);
                    _pickerValue.x = DevGUI.Slider("Saturation", _pickerValue.x, 0, 1);
                    _pickerValue.y = DevGUI.Slider("Brightness", _pickerValue.y, 0, 1);
                    if (GUI.changed)
                    {
                        UpdateColor(true);
                        GUI.changed = false;
                    }
                    break;
            }

            _color.a = DevGUI.Slider("Alpha", _color.a, 0, 1);
            if (GUI.changed)
            {
                UpdateHex();
                _dirty = true;
                GUI.changed = false;
            }
        }

        protected override void PopupGUI()
        {
            HeaderGUI();
            GUILayout.Space(PickerAreaMargin);
            GUILayout.BeginHorizontal();
            var pickerArea = GUILayoutUtility.GetRect(100, 100);
            DrawPass(pickerArea, Pass.PickerArea);
            HandlePickerArea(pickerArea);

            GUILayout.Space(PickerAreaMargin);
            _hue = HueSlider(_hue);
            if (GUI.changed)
            {
                UpdateColor(true);
                GUI.changed = false;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(PickerAreaMargin);
            SlidersGUI();

            if (GUILayout.Button("Reset"))
            {
                _color = _prevColor;
                RefreshPicker(true);
            }
        }

        private static void DrawPass(Rect rect, int pass)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            Graphics.DrawTexture(rect, Texture2D.whiteTexture, _material, pass);
        }

        public static void DrawColorRect(Rect rect, Color color)
        {
            _material.SetFloat(AspectPropId, rect.width / rect.height);
            _material.color = color;
            DrawPass(rect, Pass.Color);
        }

        public static bool TryGetColor(int id, out Color color)
        {
            color = Color.white;
            if (_instance == null || _instance.Id != id)
                return false;
            if (!_instance._dirty)
                return false;

            color = _instance._color;
            _instance._dirty = false;
            return true;
        }

        public static void Show(int id, Color color)
        {
            if (_instance == null)
            {
                var rect = new Rect(0, 10, 200, 200);
                rect.x = DevGUI.ScreenRect.center.x - rect.width * 0.5f;
                _instance = new ColorPicker(rect);
            }

            _instance._rect = DevGUIUtility.ClampPosInsideRect(_instance._rect, DevGUI.ScreenRect);
            _instance.Id = id;
            _instance._color = color;
            _instance._prevColor = color;
            _instance.RefreshPicker();
            _instance.Show(id);
        }
    }
}
