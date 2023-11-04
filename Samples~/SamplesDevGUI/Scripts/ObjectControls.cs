using UnityEngine;
using DevTools;

namespace DevGUIExample
{
    public class ObjectControls : MonoBehaviour
    {
        private float _rotation;

        private void OnEnable() => DevGUI.AddGUI("Objects", OnDevGUI);
        private void OnDisable() => DevGUI.RemoveGUI("Objects", OnDevGUI);

        private void OnDevGUI()
        {
            GUILayout.Label(name);
            _rotation = DevGUI.Slider("Rotation", _rotation, 0, 360);
            if (GUILayout.Button("Random"))
                _rotation = Random.Range(0, 360);

            if (GUI.changed)
                transform.rotation = Quaternion.Euler(0, _rotation, 0);

            if (GUILayout.Button("Destroy"))
                Destroy(gameObject);
        }
    }
}
