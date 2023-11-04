using System;
using UnityEngine;
using DevTools;

namespace DevGUIExample
{
    public class Example : MonoBehaviour
    {
        public enum Options { Option1, Option2, Option3 }

        [Flags]
        public enum FlagsEnum
        {
            None = 0,
            Flag1 = 1 << 0,
            Flag2 = 1 << 1,
            Flag3 = 1 << 2,
            Flag4 = 1 << 3
        }

        public bool toggle;
        public int intValue;
        public float floatValue;
        public float slider;
        public Color color = Color.white;
        public Options options;
        public FlagsEnum flags;

        public CameraSettings cameraSettings;

        private void OnEnable()
        {
            DevGUI.AddGUI("Example", OnDevGUI);
            DevGUI.AddGUI("Example/Sub Folder", OnDevGUIFolder);
            DevGUI.AddGUI("", OnDevGUIRoot);
        }

        private void OnDisable()
        {
            DevGUI.RemoveGUI("", OnDevGUIRoot);
            DevGUI.RemoveGUI("Example/Sub Folder", OnDevGUIFolder);
            DevGUI.RemoveGUI("Example", OnDevGUI);
        }

        private void OnDevGUIRoot()
        {
            cameraSettings.enabled = GUILayout.Toggle(cameraSettings.enabled, "Camera Settings");
        }

        private void OnDevGUI()
        {
            toggle = GUILayout.Toggle(toggle, "Bool");
            intValue = DevGUI.IntField("Int", intValue);
            floatValue = DevGUI.FloatField("Float", floatValue);
            slider = DevGUI.Slider("Float Slider", slider, 0, 1);
            color = DevGUI.ColorField("Color", color);
            options = DevGUI.EnumField("enum", options);
            flags = DevGUI.EnumField("flags", flags);
        }

        private void OnDevGUIFolder()
        {
            GUILayout.Label("Example Folder");
        }
    }
}
