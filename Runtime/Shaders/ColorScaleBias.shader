Shader "Hidden/XRCompositionLayers/ColorScaleBias"
{
    Properties
    {
        _MainTex ("Screen Texture", 2D) = "white" {}
        _ColorScale ("Color Scale", Color) = (1,1,1,1)
        _ColorBias ("Color Bias", Color) = (0,0,0,0)
    }
    HLSLINCLUDE
        /// <summary>
        /// Color transformation function that applies scale and bias to a given color.
        /// </summary>
        inline float4 ApplyColorScaleBias(float4 color, float4 scale, float4 bias)
        {
            color.rgb = color.rgb * scale.rgb + bias.rgb;
            color.a   = saturate(color.a * scale.a + bias.a);
            return color;
        }
    ENDHLSL

    // Universal Render Pipeline (URP) implementation
    SubShader
    {
        PackageRequirements
        {
            "com.unity.render-pipelines.core": "0.0"
            "com.unity.render-pipelines.universal" : "0.0"
        }
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "ForceNoShadowCasting" = "True"
            "IgnoreProjector" = "True"
        }
        LOD 100 // Level of Detail for shader
        Lighting Off
        Fog { Mode Off }
        ZWrite Off // Depth writing disabled for overlay effects
        Cull Off // Render both sides
        ZTest Always // Draw over existing content
        Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending
        Pass
        {
            Name "ColorScaleBiasEmulation_URP"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #define COMPOSITION_LAYERS_UNIVERSAL 1

    #if COMPOSITION_LAYERS_UNIVERSAL
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #endif

            float4 _ColorScale;
            float4 _ColorBias;
            /// <summary>
            /// Fragment shader for URP implementation
            /// Uses built-in blit texture sampling
            /// </summary>
            float4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // Sample screen texture with proper filtering
                float4 sceneColor = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);
                return ApplyColorScaleBias(sceneColor, _ColorScale, _ColorBias);
            }
            ENDHLSL
        }
    }
}
