﻿Shader "Hidden/CameraEffect_Outline"
{
	Properties
	{
		[PreRenderData]_MainTex ("Texture", 2D) = "white" {}
	}
		SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			HLSLPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ _CONVOLUTION_SOBEL
			#pragma multi_compile _ _DETECT_COLOR _DETECT_NORMAL
			#pragma shader_feature _COLORREPLACE
			#pragma shader_feature _NORMALDETECT
			
			#include "../CommonInclude.hlsl"
			#include "CameraEffectInclude.hlsl"
			half4 _OutlineColor;
			half _OutlineWidth;
			half _Bias;
			half4 _ReplaceColor;

			struct v2f
			{
				half4 positionCS : SV_POSITION;
				half2 uv[9] : TEXCOORD0;
			};

			v2f vert (a2v_img v)
			{
				v2f o;
				o.positionCS = TransformObjectToHClip(v.positionOS);
				half2 uv = v.uv;
				half2 pixelOffset=_OutlineWidth*_MainTex_TexelSize.xy;
                o.uv[0] = uv + pixelOffset * half2(-1, -1);
                o.uv[1] = uv + pixelOffset * half2(0, -2);
                o.uv[2] = uv + pixelOffset * half2(1, -1);
                o.uv[3] = uv + pixelOffset * half2(-2, 0);
                o.uv[4] = uv + pixelOffset * half2(0, 0);
                o.uv[5] = uv + pixelOffset * half2(2, 0);
                o.uv[6] = uv + pixelOffset * half2(-1, 1);
                o.uv[7] = uv + pixelOffset * half2(0, 2);
                o.uv[8] = uv + pixelOffset * half2(1, 1);
				return o;
			}


			float4 frag (v2f i) : SV_Target
			{
				#if _CONVOLUTION_SOBEL
				const half Gx[9]={-1,-2,-1,0,0,0,1,2,1};
				const half Gy[9]={-1,0,1,-2,0,2,-1,0,1};
				#else
				const half Gx[9]={-1,-1,-1,0,0,0,1,1,1};
				const half Gy[9]={-1,0,1,-1,0,1,-1,0,1};
				#endif

				half edgeX=0;
				half edgeY=0;
				for (int it = 0; it < 9; it++)
				{
					half diff=0;
					#if _DETECT_COLOR
					diff=luminance(tex2D(_MainTex,i.uv[it]));
					#elif _DETECT_NORMAL
					diff=abs(dot(ClipSpaceNormalFromDepth(i.uv[it]),float3(0,0,-1)));
					#else
					diff=Linear01Depth(i.uv[it]);
					#endif
					edgeX+=diff*Gx[it];
					edgeY+=diff*Gy[it];
				}
				half edgeDetect=step(_Bias,abs(edgeX)+abs(edgeY));


				float4 outlineColor=_OutlineColor;
				outlineColor.a*=edgeDetect;
				#if _COLORREPLACE
				return AlphaBlend(_ReplaceColor,outlineColor);
				#else
				return AlphaBlend(SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv[4]),outlineColor);
				#endif
			}
			ENDHLSL
		}
	}
}
