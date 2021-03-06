#ifndef UNIVERSAL_LB_LIT_PASS_INCLUDED
#define UNIVERSAL_LB_LIT_PASS_INCLUDED

struct Attributes
{
    float4 position     : POSITION;
    //float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    //float2 uv           : TEXCOORD0;
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};



Varyings Vertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    //output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    output.positionCS = TransformObjectToHClip(input.position.xyz);
    return output;
}

half4 Fragment(Varyings input) : SV_Target
{
    return half4(0,0,0,0);
}


#endif // UNIVERSAL_INPUT_SURFACE_PBR_INCLUDED
