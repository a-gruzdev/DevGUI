Shader "Hidden/DevGUI/ColorPicker"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Background("Background", 2D) = "white" {}
        _Hue ("Hue", Range(0, 1)) = 0.5
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Aspect ("Aspect", float) = 1
    }
    SubShader
    {
        Lighting Off
        Blend SrcAlpha OneMinusSrcAlpha, One One
        Cull Off
        ZWrite Off
        ZTest Always

        CGINCLUDE

        #pragma fragment frag
        #include "UnityCG.cginc"

        sampler2D _MainTex;
        sampler2D _GUIClipTexture;
        float4x4 unity_GUIClipTextureMatrix;
        sampler2D _Background;
        half _Hue;
        half4 _Color;
        float _Aspect;

        half3 gamma2linear(half3 col)
        {
        #if !defined(UNITY_COLORSPACE_GAMMA) && !defined(DEVGUI_GAMMA_CORRECT)
            return GammaToLinearSpace(col.rgb);
        #endif
            return col;
        }

        half3 hsv2rgb(half3 c)
        {
            half4 k = half4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            half3 p = abs(frac(c.xxx + k.xyz) * 6.0 - k.www);
            return c.z * lerp(k.xxx, clamp(p - k.xxx, 0.0, 1.0), c.y);
        }

        struct v2f
        {
            float4 vertex : SV_POSITION;
            fixed4 color : COLOR;
            float2 texcoord : TEXCOORD0;
            float2 clipUV : TEXCOORD1;
            UNITY_VERTEX_OUTPUT_STEREO
        };


        v2f vert(appdata_img v)
        {
            v2f o;
            UNITY_INITIALIZE_OUTPUT(v2f, o);
            UNITY_SETUP_INSTANCE_ID(v);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

            o.vertex = UnityObjectToClipPos(v.vertex);
            o.texcoord = v.texcoord;
            o.texcoord.x *= _Aspect;

            float3 eyePos = UnityObjectToViewPos(v.vertex);
            o.clipUV = mul(unity_GUIClipTextureMatrix, float4(eyePos.xy, 0, 1.0));

            return o;
        }

        ENDCG

        Pass
        {
            Name "Picker Area"
            CGPROGRAM
            #pragma multi_compile_local _ DEVGUI_GAMMA_CORRECT
            #pragma vertex vert_img

            half4 frag(v2f_img i) : SV_Target
            {
                half3 hsv = half3(_Hue, i.uv.x, i.uv.y);
                half4 col = half4(hsv2rgb(hsv), 1);
                col.rgb = gamma2linear(col.rgb);
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "Hue Area"
            CGPROGRAM
            #pragma multi_compile_local _ DEVGUI_GAMMA_CORRECT
            #pragma vertex vert_img

            half4 frag(v2f_img i) : SV_Target
            {
                half3 hsv = half3(1 - i.uv.y, 1, 1);
                half4 col = half4(hsv2rgb(hsv), 1);
                col.rgb = gamma2linear(col.rgb);
                return col;
            }
            ENDCG
        }

        Pass
        {
            Name "Color"
            CGPROGRAM
            #pragma multi_compile_local _ DEVGUI_GAMMA_CORRECT
            #pragma vertex vert

            half4 frag(v2f i) : SV_Target
            {
                half4 tex = tex2D(_Background, i.texcoord);
                tex.rgb = lerp(tex.rgb, _Color.rgb, _Color.a);

            #ifdef DEVGUI_GAMMA_CORRECT
                tex.rgb = LinearToGammaSpace(tex.rgb);
            #endif

                tex.a *= tex2D(_GUIClipTexture, i.clipUV).a;
                return tex;
            }
            ENDCG
        }
    }
}
