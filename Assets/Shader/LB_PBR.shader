Shader "LB/PBR"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1,1,1,1)
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _Metallic("Metallic", Range(0,1)) = 0.2
		_Smoothness("Smoothness(Gloss)", Range(0,1)) = 0.2

        _MetallicGlossMap("Metallic Gloss AO Mask",2D) = "white" {}  

        _BumpScale("Scale", Float) = 1.0     
        _BumpMap("Normal Map", 2D) = "bump" {} 


        _EmissionColor("Color", Color) = (0,0,0)
        _EmissionMap("Emission", 2D) = "white" {}
        [Toggle(_EMISSION_EDGE)] _EmissionEdge("EmissionEdge",float) = 0
        _EmissionEdgeWidth("Emission Edge Width", Range(0,1)) = 0.5

		[Toggle(_OL_NO_FIT)] _OutlineNoFit ("Outline NoScreenFit", Float ) = 0
		_OutlineColor("Outline Color", Color) = (1,1,1,1)
		_OutlineWidth("Outline Width", Range(0, 10)) = 0.3
		_OutlineStencilID("Stencil ID", Int) = 16

        [ToggleOff] _SpecularHighlights("Specular Highlights", Float) = 1.0
        [ToggleOff] _EnvironmentReflections("Environment Reflections", Float) = 1.0

        // Blending state
        [HideInInspector] _Surface("__surface", Float) = 0.0
        [HideInInspector] _Blend("__blend", Float) = 0.0
        [HideInInspector] _AlphaClip("__clip", Float) = 0.0
        [HideInInspector] _SrcBlend("__src", Float) = 1.0
        [HideInInspector] _DstBlend("__dst", Float) = 0.0
        [HideInInspector] _ZWrite("__zw", Float) = 1.0
        [HideInInspector] _Cull("__cull", Float) = 2.0

        [ToggleOff] _ReceiveShadows("Receive Shadows", Float) = 1.0
        // Editmode props
        [HideInInspector] _QueueOffset("Queue offset", Float) = 0.0
        [HideInInspector] [Toggle] _EditMoreOptions("Edit More Options",Float) = 0.0
        [HideInInspector] [IntRange] _SettingQualityLevel("SettingQualityLevel",Range(0,1)) = 0.0
        [HideInInspector] _ExtraPassMask("Extra Pass Mask",Float) = 1
    }

    HLSLINCLUDE

    #if  (defined(_ADDITIONAL_LIGHTS_VERTEX) || defined(_ADDITIONAL_LIGHTS) || defined(_ADDITIONAL_LIGHT_SHADOWS)) 
        #if defined(_SETTING_QUALITY_LEVEL_1) 
            #undef _ADDITIONAL_LIGHTS_VERTEX
            #undef _ADDITIONAL_LIGHTS
            #undef _ADDITIONAL_LIGHT_SHADOWS
        #endif
    #endif

    ENDHLSL


    SubShader
    {
        Tags{"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}
        LOD 300

        Pass {
		   Name "Outline_Stencil"
           Tags { "LightMode"="Outline_Stencil" }
           Cull Off
           ZWrite Off	
           ColorMask 0

           Stencil {
                Ref [_OutlineStencilID]
                Comp Always
                Pass Replace
                ZFail Replace	
            }

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile_instancing

            #include "LBLitInput.hlsl"
            #include "LBLitEmptyPass.hlsl"
            ENDHLSL
		}


        Pass {
			Name "Outline"
			Tags { "LightMode"="Outline" }
            Blend[_SrcBlend][_DstBlend]
			Cull Off		
			ZWrite[_ZWrite]

			Stencil {
				Ref [_OutlineStencilID]
				Comp NotEqual
				Pass Keep
				ZFail Keep
			}

    

			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			
			#pragma shader_feature _OL_NO_FIT
            #pragma shader_feature TCP2_TANGENT_AS_NORMALS
            #pragma shader_feature TCP2_COLORS_AS_NORMALS
            #pragma multi_compile_instancing


            #include "LBLitInput.hlsl"

            struct Attributes
            {
                float4 position     : POSITION;
                //float2 texcoord     : TEXCOORD0;
                float3 normalOS : NORMAL;

                #if defined(TCP2_COLORS_AS_NORMALS)
	                float4 vertexColor : COLOR;
                #endif

                #if defined(TCP2_TANGENT_AS_NORMALS)
	                float4 tangent : TANGENT;
                #endif
        
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                //float2 uv           : TEXCOORD0;
                float4 positionCS   : SV_POSITION;
                float fogCoord  : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

			Varyings vert(Attributes input)
			{
				Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

 
                half3 posOS = input.position.xyz;
                half4 positionCS = TransformObjectToHClip(posOS);



                #if TCP2_TANGENT_AS_NORMALS
		            half3 normalOS = input.tangent.xyz;
                #else 
                    half3 normalOS = input.normalOS;  
                #endif


				half3 normalCS = SafeNormalize( TransformWorldToHClipDir(TransformObjectToWorldNormal(normalOS)));
				
                /*
			    normalCS.xy *= positionCS.w;
                half outlineWidth = _OutlineWidth;
                */
                normalCS.xy = (normalCS.xy / _ScreenParams.xy) * 2.0;
			    half outlineWidth = _OutlineWidth * 10;

				positionCS.xy += normalCS.xy * outlineWidth;

                /*
				#if defined(SHADER_API_GLCORE) || defined(SHADER_API_GLES) || defined(SHADER_API_GLES3)
					positionCS.z = positionCS.z + _OutlineZPostionInCamera * 0.0005;
				#else
					positionCS.z = positionCS.z - _OutlineZPostionInCamera * 0.0005;
				#endif      
                */
                output.positionCS = positionCS;
                output.fogCoord = ComputeFogFactor(positionCS.z);

				return output;
			}

			half4 frag(Varyings input) : SV_Target
			{
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

				half4 color = _OutlineColor;

				color.rgb = MixFog(color.rgb, input.fogCoord);

				return color;
			}

			ENDHLSL
		}


        Pass {
            Name "ZWriteAlways"
			Tags { "LightMode" = "ZWriteAlways" }

			Offset 1,1
			ZWrite On
			ColorMask 0

            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #pragma multi_compile_instancing

            #include "LBLitInput.hlsl"
            #include "LBLitEmptyPass.hlsl"
            ENDHLSL
		}

        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode" = "UniversalForward"}

            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]
            Cull[_Cull]

            HLSLPROGRAM

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _METALLICSPECGLOSSMAP
            #pragma shader_feature _NORMALMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _ALPHAPREMULTIPLY_ON
            #pragma shader_feature _EMISSION
            #pragma shader_feature _EMISSION_EDGE
            #pragma shader_feature _EMISSION_EDGE_GHOST
            #pragma shader_feature _OCCLUSIONMAP

            #pragma shader_feature _SPECULARHIGHLIGHTS_OFF
            #pragma shader_feature _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature _RECEIVE_SHADOWS_OFF
            #pragma shader_feature _SETTING_QUALITY_LEVEL_1
           

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _MIXED_LIGHTING_SUBTRACTIVE


            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile_fog

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing


            #pragma vertex LitPassVertex
            #pragma fragment Fragment


            
            #include "LBLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"


            uniform half _GrayScale;

            
            half4 Fragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);


                InputData inputData;
                InitializeInputData(input, surfaceData.normalTS, inputData);


                #if defined(_EMISSION) && defined(_EMISSION_EDGE) 
                    half nDotV = dot (inputData.normalWS , inputData.viewDirectionWS);
                    half sRim = saturate(1 -nDotV);

                    #if _EMISSION_EDGE_GHOST
                        half rim = smoothstep(surfaceData.alpha*(1+_EmissionEdgeWidth)-_EmissionEdgeWidth, 1.0h,sRim );
                        surfaceData.emission *= rim;
                        surfaceData.alpha += sRim * _EmissionEdgeWidth*0.5h;
                    #else
                        half rim = smoothstep(_EmissionEdgeWidth, 1.0h,sRim );
                        surfaceData.emission *= rim;
                    #endif
                #endif

                half4 color = UniversalFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

                color.rgb = MixFog(color.rgb, inputData.fogCoord);
                
                color.rgb = lerp(color.rgb, dot(color.rgb, half3(0.3h, 0.59h, 0.11h)), _GrayScale);

                return color;
            }

            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode" = "ShadowCaster"}

            ZWrite On
            ZTest LEqual
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON

            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing


            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment

            #include "LBLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
 
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode" = "DepthOnly"}

            ZWrite On
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #pragma vertex DepthOnlyVertex
            #pragma fragment Fragment

            // -------------------------------------
            // Material Keywords
            #pragma shader_feature _ALPHATEST_ON


            //--------------------------------------
            // GPU Instancing
            #pragma multi_compile_instancing

            #include "LBLitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"

            half4 Fragment(Varyings input) : SV_TARGET
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);

    
                half alpha = _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                return 0;
            }
            ENDHLSL
        }

        // This pass it not used during regular rendering, only for lightmap baking.
        Pass
        {
            Name "Meta"
            Tags{"LightMode" = "Meta"}

            Cull Off

            HLSLPROGRAM
            // Required to compile gles 2.0 with standard srp library
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #pragma vertex UniversalVertexMeta
            #pragma fragment FragmentMeta

            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICSPECGLOSSMAP
            #pragma shader_feature _ALPHATEST_ON
            #pragma shader_feature _SETTING_QUALITY_LEVEL_1


            #include "LBLitInput.hlsl"         
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitMetaPass.hlsl"


            half4 FragmentMeta(Varyings input) : SV_Target
            {
                SurfaceData surfaceData;
                InitializeStandardLitSurfaceData(input.uv, surfaceData);

                BRDFData brdfData;
                InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

                MetaInput metaInput;
                metaInput.Albedo = brdfData.diffuse + brdfData.specular * brdfData.roughness * 0.5;
                metaInput.SpecularColor = surfaceData.specular;
                metaInput.Emission = surfaceData.emission;

                return MetaFragment(metaInput);
            }
            ENDHLSL
        }
     
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.LBLitShader"
}
