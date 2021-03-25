#ifndef UNIVERSAL_LB_LIT_INPUT_INCLUDED
#define UNIVERSAL_LB_LIT_INPUT_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

CBUFFER_START(UnityPerMaterial)
float4 _BaseMap_ST;
half4 _BaseColor;
half4 _EmissionColor;
half _Cutoff;
half _Smoothness;
half _Metallic;
half _BumpScale;
half _EmissionEdgeWidth;
half _Surface;

half4 _OutlineColor;
half _OutlineWidth;
CBUFFER_END


TEXTURE2D(_MetallicGlossMap);   SAMPLER(sampler_MetallicGlossMap);

half4 SampleMetallicGlossAO(float2 uv, half albedoAlpha)
{
    half4 metaGloss;
#if defined(_METALLICSPECGLOSSMAP) 
    metaGloss = SAMPLE_TEXTURE2D(_MetallicGlossMap, sampler_MetallicGlossMap, uv);
    #if !defined(SHADER_API_GLES)
    metaGloss.b = LerpWhiteTo(metaGloss.b, 1);
    #endif
#else
    metaGloss.r = _Metallic;
    metaGloss.g = _Smoothness;
    metaGloss.b = 1;
#endif
    return metaGloss;
}


inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData outSurfaceData)
{
    half4 albedoAlpha = SampleAlbedoAlpha(uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    outSurfaceData.alpha = Alpha(albedoAlpha.a, _BaseColor, _Cutoff);

    half4 metaGloss = SampleMetallicGlossAO(uv, albedoAlpha.a);
    outSurfaceData.albedo = albedoAlpha.rgb * _BaseColor.rgb;
    outSurfaceData.specular = half3(0.0h, 0.0h, 0.0h);

    outSurfaceData.metallic = metaGloss.r;        
    outSurfaceData.smoothness = metaGloss.g;
    outSurfaceData.occlusion = metaGloss.b;

    outSurfaceData.normalTS = SampleNormal(uv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
     
    outSurfaceData.emission = SampleEmission(uv, _EmissionColor.rgb, TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap));
}



#endif // UNIVERSAL_LB_LIT_INPUT_INCLUDED
