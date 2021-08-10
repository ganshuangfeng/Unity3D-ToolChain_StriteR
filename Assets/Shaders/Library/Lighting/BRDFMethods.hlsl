﻿
//NDF,Normal Distribution Function
float NDF_BlinnPhong(float NDH, float specularPower, float specularGloss)
{
    float distribution = pow(NDH, specularGloss) * specularPower;
    distribution *= (2 + specularPower) / PI_TWO;
    return distribution;
}
float NDF_Phong(float RDV, float specularPower, float specularGloss)
{
    float Distribution = pow(RDV, specularGloss) * specularPower;
    Distribution *= (2 + specularPower) / PI_TWO;
    return Distribution;
}
float NDF_Beckmann(float NDH, float sqrRoughness)
{
    float sqrNDH = dot(NDH, NDH);
    return max(0.000001, (1.0 / (PI_ONE * sqrRoughness * sqrNDH * sqrNDH)) * exp((sqrNDH - 1) / (sqrNDH * sqrRoughness)));
}
float NDF_Gaussian(float NDH, float sqrRoughness)
{
    float thetaH = acos(NDH);
    return exp(-thetaH * thetaH / sqrRoughness);
}
float NDF_GGX(float NDH,float roughness, float sqrRoughness)
{
    float sqrNDH = dot(NDH, NDH);
    float tanSqrNDH = (1 - sqrNDH) / sqrNDH;
    return max ( 0.00001, PI_ONEMINUS * pow2(roughness / (sqrNDH * (sqrRoughness + tanSqrNDH))));
}
float NDF_CookTorrance(float NDH,float LDH,float roughness,float roughness2)
{
    NDH = saturate(NDH);
    LDH = saturate(LDH);
    float d = NDH * NDH *( roughness2-1.) +1.00001f;
    float sqrLDH = pow2(LDH);
    return roughness2 / (d * d);
}
float NDF_TrowbridgeReitz(float NDH, float roughness,float sqrRoughness)
{
    float sqrNDH = dot(NDH, NDH);
    float distribution = sqrNDH * (sqrRoughness - 1.0) + 1.0;
    return sqrRoughness / (PI_ONE * distribution * distribution+1e-5f);
}
//Anisotropic NDF
float NDFA_TrowbridgeReitz(float NDH, float HDX, float HDY, float anisotropic, float glossiness)
{
    float aspect = sqrt(1.0h - anisotropic * 0.9h);
    glossiness = pow2(1.0 - glossiness);
    float X = max(.001, glossiness / aspect) * 5;
    float Y = max(.001, glossiness * aspect) * 5;
    return 1.0 / (PI_ONE * X * Y * pow2(pow2(HDX / X) + pow2(HDY / Y) + pow2(NDH)));
}
float NDFA_Ward(float NDL, float NDV, float NDH, float HDX, float HDY, float anisotropic, float glossiness)
{
    float aspect = sqrt(1.0h - anisotropic * 0.9h);
    glossiness = pow2(1.0 - glossiness);
    float X = max(.001, glossiness / aspect) * 5;
    float Y = max(.001, glossiness * aspect) * 5;
    float exponent = -(pow2(HDX / X) + pow2(HDY / Y)) / pow2(NDH);
    float distribution = 1. / (PI_FOUR * X * Y) * sqrt(NDL*NDV);
    distribution *= exp(exponent);
    return distribution;
}
//VF: (VisibilityTerm * FresnelTerm) * 4.0
float InvVF_GGX(float LDH, float roughness)
{
    float sqrLDH = pow2(LDH);
    return max(0.1h, sqrLDH) * (roughness + .5);
}
float InvVF_BlinnPhong(float LDH)
{
    return max(0.1h, pow3(LDH));
}

//Fresnel
float F_Schlick(float NDV)
{ 
    float x = saturate(1. - NDV);
    return pow4(x);//pow5(x);
}
