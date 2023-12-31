using System.Reflection;
using UnityEngine;

namespace DevTools
{
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class DevGUIGammaFix : MonoBehaviour
    {
        private static Texture2D _alphaLookup;
        private Shader _shader;
        private Material _material;
        private Shader _defaultShader;

        private void Awake()
        {
            _shader = Shader.Find("Hidden/DevGUI/Internal-GUITextureClip");

            // unity's internal imgui material
            // https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/IMGUI/GUI.bindings.cs#L24
            var materialProp = typeof(GUI).GetProperty("blendMaterial", BindingFlags.Static | BindingFlags.NonPublic);
            _material = (Material)materialProp.GetValue(null);
            _defaultShader = _material.shader;

            if (_alphaLookup == null)
            {
                _alphaLookup = Resources.Load<Texture2D>("GammaAlphaLookupTex");
                _alphaLookup.hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            Shader.SetGlobalTexture("_AlphaLookupTex", _alphaLookup);
        }

        private void OnEnable()
        {
            DevGUI.OnGUIBegin += Begin;
            DevGUI.OnGUIEnd += End;
        }

        private void OnDisable()
        {
            DevGUI.OnGUIBegin -= Begin;
            DevGUI.OnGUIEnd -= End;
        }

        private void Begin() => _material.shader = _shader;
        private void End() => _material.shader = _defaultShader;
    }
}
