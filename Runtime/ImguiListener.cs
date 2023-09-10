using UnityEngine;
using UnityEngine.EventSystems;

namespace DevTools
{
    [DefaultExecutionOrder(-10000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(EventSystem))]
    public class ImguiListener : MonoBehaviour
    {
        private EventSystem _eventSystem;

        private void Awake() => _eventSystem = GetComponent<EventSystem>();
        private void Update() => _eventSystem.enabled = GUIUtility.hotControl == 0;
    }
}
