Shader "Hidden/ImageEffect_Stylize"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        HLSLINCLUDE
            #include "../CommonInclude.hlsl"
            #include "CameraEffectInclude.hlsl"
        ENDHLSL
        Pass
        {
            Name "Pixelize"
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma shader_feature _PIXEL_GRID
            float4 _PixelGridColor;
            float2 _PixelGridWidth;
            float4 frag(v2f_img i):SV_TARGET
            {
                float3 finalCol=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).xyz;
            #if _PIXEL_GRID
                float2 pixelUV= (i.uv*_MainTex_TexelSize.zw)%1;
                float pixelGrid= step(pixelUV.y,_PixelGridWidth.x)+step(_PixelGridWidth.y,pixelUV.y)+step(pixelUV.x,_PixelGridWidth.x)+step(_PixelGridWidth.y,pixelUV.x);
                finalCol=lerp(finalCol,_PixelGridColor.rgb,pixelGrid*_PixelGridColor.a);
            #endif
                return float4(finalCol,1);
            }
            ENDHLSL
        }

        Pass
        {   
            Name "Oil Paint"
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            int2 _OilPaintKernel;
            float _OilPaintSize;
            //Kuwahara Filter
            struct filterRegion
            {
                float3 mean;
                float variance;
            };
            filterRegion GetFilter(int2 lowerLeft,int2 upperRight,float2 uv)
            {
                filterRegion r;
                float3 sum=0;
                float3 squareSum=0;
                int samples=(upperRight.x-lowerLeft.x+1)*(upperRight.y-lowerLeft.y+1);
                float random=(1+random01(uv)*.2);
                for(int i=lowerLeft.x;i<=upperRight.x;i++)
                {
                    for(int j=lowerLeft.y;j<=upperRight.y;j++)
                    {
                        float2 offset=float2(i,j)*_MainTex_TexelSize.xy*_OilPaintSize*random;
                        float3 col=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,uv+offset).xyz;
                        sum+=col;
                        squareSum+=col*col;
                    }
                }
                r.mean=sum/samples;
                float3 variance=abs((squareSum/samples)-(r.mean*r.mean));
                r.variance=length(variance);
                return r;
            }

            float4 frag(v2f_img i):SV_TARGET
            {
                int lower=_OilPaintKernel.x;
                int upper=_OilPaintKernel.y;
                filterRegion regionA=GetFilter(int2(lower,lower),int2(0,0),i.uv);
                filterRegion regionB=GetFilter(int2(0,lower),int2(upper,0),i.uv);
                filterRegion regionC=GetFilter(int2(lower,0),int2(0,upper),i.uv);
                filterRegion regionD=GetFilter(int2(0,0),int2(upper,upper),i.uv);
                
                float3 col = regionA.mean;
                float minVar = regionA.variance;
                
                float testVal = step(regionB.variance, minVar);
                col = lerp(col, regionB.mean, testVal);
                minVar = lerp(minVar, regionB.variance, testVal);

                testVal = step(regionC.variance, minVar);
                col = lerp(col, regionC.mean, testVal);
                minVar = lerp(minVar, regionC.variance, testVal);

                testVal = step(regionD.variance, minVar);
                col = lerp(col, regionD.mean, testVal);

                return float4(col,1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Obra Dithering"
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            TEXTURE2D(_DitherMap);SAMPLER(sampler_DitherMap);
            float _ObraDitherScale;
            float _ObraDitherStrength;
            float3 _ObraDitherColor;
            float4 frag(v2f_img i):SV_TARGET
            {
                float lum=luminance( SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv).xyz);

                i.uv*=_MainTex_TexelSize.zw*_ObraDitherScale;
                i.uv=floor(i.uv)*.1;

                lum=1-step( lum,random01(i.uv)*_ObraDitherStrength);


                return float4(lerp(_ObraDitherColor,1,lum) ,1);
            }
            ENDHLSL
        }
    }
}