using UnityEngine;

namespace DevTools
{
    public class Example : MonoBehaviour
    {
        public GameObject target;
        public float angle;
        public string text = "lol";

        private void OnEnable() => DevGUI.AddGUI("Example", OnDevGUI);
        private void OnDisable() => DevGUI.RemoveGUI("Example", OnDevGUI);

        private void OnDevGUI()
        {
            var value = GUILayout.Toggle(target.activeSelf, "Toggle Example");
            if (value != target.activeSelf)
                target.SetActive(value);

            angle = DevGUI.Slider("Angle", angle, 0, 360);
            target.transform.rotation = Quaternion.Euler(0, angle, 0);

            if (GUILayout.Button("Move Up")) target.transform.position += Vector3.up;
            if (GUILayout.Button("Move Down")) target.transform.position += Vector3.down;

            text = DevGUI.TextField("Text Field", text);
        }
    }
}
