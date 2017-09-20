//Helper to disable bounding box compute code
#define USE_DYNAMIC_AABB 1

// Pi variables are redefined here as UnityCG.cginc is not included for compute shader as it adds too many unused uniforms to constant buffers
#ifndef UNITY_CG_INCLUDED
#define UNITY_PI            3.14159265359f
#define UNITY_TWO_PI        6.28318530718f
#define UNITY_FOUR_PI       12.56637061436f
#define UNITY_INV_PI        0.31830988618f
#define UNITY_INV_TWO_PI    0.15915494309f
#define UNITY_INV_FOUR_PI   0.07957747155f
#define UNITY_HALF_PI       1.57079632679f
#define UNITY_INV_HALF_PI   0.636619772367f
#endif

// Special semantics for VFX blocks
#define RAND randLcg(seed)
#define RAND2 float2(RAND,RAND)
#define RAND3 float3(RAND,RAND,RAND)
#define RAND4 float4(RAND,RAND,RAND,RAND)
#define FIXED_RAND(s) FixedRand4(particleId ^ s).x
#define FIXED_RAND2(s) FixedRand4(particleId ^ s).xy
#define FIXED_RAND3(s) FixedRand4(particleId ^ s).xyz
#define FIXED_RAND4(s) FixedRand4(particleId ^ s).xyzw
#define KILL {kill = true;}
#define SAMPLE sampleSignal
#define SAMPLE_SPLINE_POSITION(v,u) sampleSpline(v.x,u)
#define SAMPLE_SPLINE_TANGENT(v,u) sampleSpline(v.y,u)
#define INVERSE(m) Inv##m

struct VFXSampler2D
{
    Texture2D t;
    SamplerState s;
};

struct VFXSampler3D
{
    Texture3D t;
    SamplerState s;
};

// indices to access to system data
#define VFX_DATA_UPDATE_ARG_GROUP_X     0
#define VFX_DATA_RENDER_ARG_NB_INDEX    4
#define VFX_DATA_RENDER_ARG_NB_INSTANCE 5
#define VFX_DATA_NB_CURRENT             8
#define VFX_DATA_NB_INIT                9
#define VFX_DATA_NB_UPDATE              10
#define VFX_DATA_NB_FREE                11

#ifdef VFX_WORLD_SPACE // World Space
float3 VFXCameraPos()                   { return _WorldSpaceCameraPos.xyz; }
float3 VFXCameraLook()                  { return -unity_WorldToCamera[2].xyz; }
float4x4 VFXCameraMatrix()              { return unity_WorldToCamera; }
float VFXNearPlaneDist(float3 position) { return mul(unity_WorldToCamera,float4(position,1.0f)).z; }
float4x4 VFXModelViewProj()             { return UNITY_MATRIX_VP; }
#elif defined(VFX_LOCAL_SPACE) // Local space
float3 VFXCameraPos()                   { return mul(unity_WorldToObject,float4(_WorldSpaceCameraPos.xyz,1.0)).xyz; }
float3 VFXCameraLook()                  { return UNITY_MATRIX_MV[2].xyz; }
float4x4 VFXCameraMatrix()              { return UNITY_MATRIX_MV; }
float VFXNearPlaneDist(float3 position) { return -mul(UNITY_MATRIX_MV,float4(position,1.0f)).z; }
float4x4 VFXModelViewProj()             { return UNITY_MATRIX_MVP; }
#endif

// Macros to use Sphere semantic type directly
#define VFXPositionOnSphere(SphereName,cosPhi,theta,rNorm)  PositionOnSphere(SphereName##_center,SphereName##_radius,cosPhi,theta,rNorm)
#define VFXPositionOnSphereSurface(SphereName,cosPhi,theta) PositionOnSphereSurface(SphereName##_center,SphereName##_radius,cosPhi,theta)

// center,radius: Sphere description
// cosPhi: cosine of Phi angle (we used the cosine directly as it used for uniform distribution)
// theta: theta angle
// rNorm: normalized radius in the sphere where the point lies
float3 PositionOnSphere(float3 center,float radius,float cosPhi,float theta,float rNorm)
{
    float2 sincosTheta;
    sincos(theta,sincosTheta.x,sincosTheta.y);
    sincosTheta *= sqrt(1.0 - cosPhi*cosPhi);
    return float3(sincosTheta,cosPhi) * (rNorm * radius) + center;
}

float3 PositionOnSphereSurface(float3 center,float radius,float cosPhi,float theta)
{
    return PositionOnSphere(center,radius,cosPhi,theta,1.0f);
}

// Macros to use Cylinder semantic type directly
#define VFXPositionOnCylinder(CylinderName,hNorm,theta,rNorm)   PositionOnCylinder(CylinderName##_position,CylinderName##_direction,CylinderName##_height,CylinderName##_radius,hNorm,theta,rNorm)
#define VFXPositionOnCylinderSurface(CylinderName,hNorm,theta)  PositionOnCylinderSurface(CylinderName##_position,CylinderName##_direction,CylinderName##_height,CylinderName##_radius,hNorm,theta)

// pos,dir,height,radius: Cylinder description
// hNorm: normalized height for the point in [-0.5,0.5]
// theta: theta angle
// rNorm: normalise radius for the point
float3 PositionOnCylinder(float3 pos,float3 dir,float height,float radius,float hNorm,float theta,float rNorm)
{
    float2 sincosTheta;
    sincos(theta,sincosTheta.x,sincosTheta.y);
    sincosTheta *= rNorm * radius;
    float3 normal = normalize(cross(dir,dir.zxy));
    float3 binormal = cross(normal,dir);
    return normal * sincosTheta.x + binormal * sincosTheta.y + dir * (hNorm * height) + pos;
}

float3 PositionOnCylinderSurface(float3 pos,float3 dir,float height,float radius,float hNorm,float theta)
{
    return PositionOnCylinder(pos,dir,height,radius,hNorm,theta,1.0f);
}

float4 SampleTexture(VFXSampler2D s,float2 coords)
{
    return s.t.SampleLevel(s.s,coords,0.0f);
}

float4 SampleTexture(VFXSampler3D s,float3 coords)
{
    return s.t.SampleLevel(s.s,coords,0.0f);
}

VFXSampler2D InitSampler(Texture2D t,SamplerState s)
{
    VFXSampler2D vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

VFXSampler3D InitSampler(Texture3D t,SamplerState s)
{
    VFXSampler3D vfxSampler;
    vfxSampler.t = t;
    vfxSampler.s = s;
    return vfxSampler;
}

uint ConvertFloatToSortableUint(float f)
{
    int mask = (-(int)(asuint(f) >> 31)) | 0x80000000;
    return asuint(f) ^ mask;
}

uint3 ConvertFloatToSortableUint(float3 f)
{
    uint3 res;
    res.x = ConvertFloatToSortableUint(f.x);
    res.y = ConvertFloatToSortableUint(f.y);
    res.z = ConvertFloatToSortableUint(f.z);
    return res;
}

/////////////////////////////
// Random number generator //
/////////////////////////////

uint WangHash(uint seed)
{
    seed = (seed ^ 61) ^ (seed >> 16);
    seed *= 9;
    seed = seed ^ (seed >> 4);
    seed *= 0x27d4eb2d;
    seed = seed ^ (seed >> 15);
    return seed;
}

float randLcg(inout uint seed)
{
    uint multiplier = 0x0019660d;
    uint increment = 0x3c6ef35f;
#if 1
    seed = multiplier * seed + increment;
    return asfloat((seed >> 9) | 0x3f800000) - 1.0f;
#else //Using mad24 keeping consitency between platform
    #if defined(SHADER_API_PSSL)
        seed = mad24(multiplier, seed, increment);
    #else
        seed = multiplier * seed + increment;
    #endif
    //Using >> 9 instead of &0x007fffff seems to lead to a better random, but with this way, the result is the same between PS4 & PC
    //We need to find a LCG considering the mul24 operation instead of mul32
    //possible variant : return float(seed & 0x007fffff) / float(0x007fffff)
    return asfloat((seed & 0x007fffff) | 0x3f800000) - 1.0f;
#endif
}

float4 FixedRand4(uint baseSeed)
{
    uint currentSeed = WangHash(baseSeed);
    float4 r;
    [unroll(4)]
    for (uint i=0; i<4; ++i)
    {
        r[i] = randLcg(currentSeed);
    }
    return r;
}

///////////////////////////
// Color transformations //
///////////////////////////

float3 HUEtoRGB(in float H)
{
    float R = abs(H * 6 - 3) - 1;
    float G = 2 - abs(H * 6 - 2);
    float B = 2 - abs(H * 6 - 4);
    return saturate(float3(R,G,B));
}

float3 RGBtoHCV(in float3 RGB)
{
    float4 P = (RGB.g < RGB.b) ? float4(RGB.bg, -1.0, 2.0/3.0) : float4(RGB.gb, 0.0, -1.0/3.0);
    float4 Q = (RGB.r < P.x) ? float4(P.xyw, RGB.r) : float4(RGB.r, P.yzx);
    float C = Q.x - min(Q.w, Q.y);
    float H = abs((Q.w - Q.y) / (6 * C + 1e-10) + Q.z);
    return float3(H, C, Q.x);
}

float3 RGBtoHSV(in float3 RGB)
{
    float3 HCV = RGBtoHCV(RGB);
    float S = HCV.y / (HCV.z + 1e-10);
    return float3(HCV.x, S, HCV.z);
}

float3 HSVtoRGB(in float3 HSV)
{
    return ((HUEtoRGB(HSV.x) - 1) * HSV.y + 1) * HSV.z;
}

///////////////////
// Baked texture //
///////////////////

Texture2D bakedTexture;
SamplerState samplerbakedTexture;

float HalfTexelOffset(float f)
{
    const uint kTextureWidth = 128;
    float a = (kTextureWidth - 1.0f) / kTextureWidth;
    float b = 0.5f / kTextureWidth;
    return (a * f) + b;
}

float4 SampleGradient(float v,float u)
{
    float uv = float2(HalfTexelOffset(saturate(u)),v);
    return bakedTexture.SampleLevel(samplerbakedTexture,uv,0);
}

float SampleCurve(float4 curveData,float u)
{
    float uNorm = (u * curveData.x) + curveData.y;
    switch(asuint(curveData.w) >> 2)
    {
        case 1: uNorm = HalfTexelOffset(frac(min(1.0f - 1e-10f,uNorm))); break; // clamp end. Dont clamp at 1 or else the frac will make it 0...
        case 2: uNorm = HalfTexelOffset(frac(max(0.0f,uNorm))); break; // clamp start
        case 3: uNorm = HalfTexelOffset(saturate(uNorm)); break; // clamp both
    }
    return bakedTexture.SampleLevel(samplerbakedTexture,float2(uNorm,curveData.z),0)[asuint(curveData.w) & 0x3];
}
