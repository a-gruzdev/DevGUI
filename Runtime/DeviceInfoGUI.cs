using UnityEngine;

namespace DevTools
{
    public class DeviceInfoGUI : MonoBehaviour
    {
        private const string Title = "Device Info";

        private void OnEnable() => DevGUI.AddGUI(Title, InfoGUI);
        private void OnDisable() => DevGUI.RemoveGUI(Title, InfoGUI);

        private void InfoGUI()
        {
            GUILayout.Label($"Device: {SystemInfo.deviceModel}");
            GUILayout.Label($"Resolution: {Screen.currentResolution}");
            GUILayout.Label($"Graphics API: {SystemInfo.graphicsDeviceType}");
            GUILayout.Label($"Graphics Device: {SystemInfo.graphicsDeviceName}");
        }
    }
}
