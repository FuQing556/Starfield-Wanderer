Shader "StarfieldWanderer/Sprites/Colorize"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _TargetColor ("Target Color", Color) = (0.78,0.86,0.92,1)
        _RecolorStrength ("Recolor Strength", Range(0,1)) = 1
        _Brightness ("Brightness", Range(0,2)) = 1
        _Contrast ("Contrast", Range(0,2)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment ColorizeSpriteFrag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA

            #include "UnitySprites.cginc"

            fixed4 _TargetColor;
            half _RecolorStrength;
            half _Brightness;
            half _Contrast;

            fixed4 ColorizeSpriteFrag(v2f input) : SV_Target
            {
                fixed4 source = SampleSpriteTexture(input.texcoord) * input.color;
                half luminance = dot(source.rgb, half3(0.299h, 0.587h, 0.114h));
                half adjustedLuminance = saturate((luminance - 0.5h) * _Contrast + 0.5h);
                half3 colorized = adjustedLuminance * _TargetColor.rgb * _Brightness;
                source.rgb = saturate(lerp(source.rgb, colorized, saturate(_RecolorStrength)));
                source.rgb *= source.a;
                return source;
            }
            ENDCG
        }
    }
}
