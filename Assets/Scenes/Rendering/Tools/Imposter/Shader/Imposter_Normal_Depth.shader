Shader "Game/Optimize/Imposter/Normal_Depth"
{
    Properties
    {
    	_AlphaClip("Clip",Range(0,1)) = 0.5
        [NoScaleOffset]_AlbedoAlpha("_AlbedoAlpha",2D) = "white"
        [NoScaleOffset]_NormalDepth("_NormalDepth",2D) = "white"
	    _Weights("Weights",Vector) = (1,1,1,1)
    	_ImposterViewDirection("Imposter View Direction",Vector) = (1,1,1,1)
    }
    SubShader
    {
    	Blend Off
    	HLSLINCLUDE
            #include "Assets/Shaders/Library/Common.hlsl"
            #include "Assets/Shaders/Library/Lighting.hlsl"
            #include "Imposter.hlsl"

            struct a2v
            {
                float3 positionOS : POSITION;
            	float4 uv0 : TEXCOORD0;
            	float4 uv1 : TEXCOORD1;
            	float4 uv2 : TEXCOORD2;
            	float4 uv3 : TEXCOORD3;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float4 uv0 : TEXCOORD0;
            	float4 uv1 : TEXCOORD1;
				float4 uv2 : TEXCOORD2;
            	float4 uv3 :TEXCOORD3;
                float3 positionWS :TEXCOORD4;
            	float4 positionHCS : TEXCOORD5;
            	float3 positionOS : TEXCOORD6;
				UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct f2o
            {
                float4 result : SV_TARGET;
                float depth : SV_DEPTH;
            };

            TEXTURE2D(_AlbedoAlpha);SAMPLER(sampler_AlbedoAlpha);
            TEXTURE2D(_NormalDepth);SAMPLER(sampler_NormalDepth);
            INSTANCING_BUFFER_START
				INSTANCING_PROP(float4,_Rotation)
            INSTANCING_BUFFER_END

            float3 quaternionMul(float4 quaternion,float3 direction)
            {
	            float3 t = 2 * cross(quaternion.xyz, direction);
				return direction + quaternion.w * t + cross(quaternion.xyz, t);
            }
            
			float3 _ImposterViewDirection;
            float _AlphaClip;
            float4 _Weights;

            
            v2f vert (a2v v)
            {
                v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.positionCS = TransformObjectToHClip(v.positionOS);
                o.positionWS = TransformObjectToWorld(v.positionOS);
            	o.positionOS = v.positionOS;
            	o.positionHCS = o.positionCS;
				o.uv0 = v.uv0;
				o.uv1 = v.uv1;
				o.uv2 = v.uv2;
				o.uv3 = v.uv3;
                return o;
            }

			float4 GetFragmentUV(v2f i,int index)
            {
	            switch (index)
	            {
		            case 0: return i.uv0;
		            case 1: return i.uv1;
		            case 2: return i.uv2;
					case 3: return i.uv3;
	            }
            	return i.uv0;
            }
            
            f2o frag (v2f i)
            {
				UNITY_SETUP_INSTANCE_ID(i);
                f2o o;
            	
                float4 albedoAlpha = 0;
				float3 normalWS = 0;
				float depthExtrude = 0;

            	for(int index=0;index<4;index++)
            	{
            		float4 uv = GetFragmentUV(i,index);
            		float4 directionNWeight = _Weights[index];
            		float weight = directionNWeight.w;
            		float bias = (1-weight) * 2;

            		float4 sample = SAMPLE_TEXTURE2D_BIAS(_AlbedoAlpha,sampler_AlbedoAlpha, uv.xy,bias);
            		albedoAlpha += sample * weight;
            		
            		float4 normalDepth = SAMPLE_TEXTURE2D_BIAS(_NormalDepth, sampler_NormalDepth, uv.xy,bias);
            		normalWS += normalDepth.rgb * weight;
            		depthExtrude += normalDepth.a * weight;
            	}
				normalWS = normalWS * 2 - 1;
				normalWS = normalize(normalWS);
				normalWS = quaternionMul(_Rotation,normalWS);
                
                float diffuse = saturate(dot(normalWS,_MainLightPosition.xyz)) ;
                
                float3 albedo = albedoAlpha.rgb;
            	clip(albedoAlpha.a-_AlphaClip);

                o.result = float4(albedo * diffuse * _MainLightColor + albedo * SHL2Sample(normalWS,unity),1);

				depthExtrude = depthExtrude * 2 -1;
                o.depth = EyeToRawDepth(TransformWorldToEyeDepth(TransformObjectToWorld(i.positionOS - _ImposterViewDirection * saturate(depthExtrude))));
                return o;
            }
        ENDHLSL
    	
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
			Tags{"LightMode" = "UniversalForward"}
            ZTest LEqual
            ZWrite On
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            ENDHLSL
        }
        
		Pass
		{
			NAME "MAIN"
			Tags{"LightMode" = "DepthOnly"}
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDHLSL
		}
    }
}
