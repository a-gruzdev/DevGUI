using UnityEngine;
using DevTools;

namespace DevGUIExample
{
    public class CameraSettings : MonoBehaviour
    {
        private Camera _camera;

        private void OnEnable()
        {
            _camera = GetComponent<Camera>();
            DevGUI.AddGUI("Camera Settings", OnDevGUI);
        }

        private void OnDisable()
        {
            DevGUI.RemoveGUI("Camera Settings", OnDevGUI);
        }

        private void OnDevGUI()
        {
            _camera.backgroundColor = DevGUI.ColorField("Background", _camera.backgroundColor);
            _camera.fieldOfView = DevGUI.Slider("Fov", _camera.fieldOfView, 20, 120);
        }
    }
}
