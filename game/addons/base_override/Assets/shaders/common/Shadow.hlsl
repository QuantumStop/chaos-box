#ifndef SHADOW_HLSL
#define SHADOW_HLSL
// Todo: make this a pretty class

#include "common/lightbinner.hlsl"

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Shadow maps
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
SamplerComparisonState  g_tShadowDepthBufferCmpSampler  < Filter( COMPARISON_MIN_MAG_MIP_LINEAR ); 	AddressU( CLAMP ); AddressV( CLAMP ); >;
SamplerState 			g_sShadowDepthSampler 			< Filter( MIN_MAG_LINEAR_MIP_POINT ); 		AddressU( CLAMP ); AddressV( CLAMP ); >;

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Shadow map
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
Texture2D g_tShadowDepthBufferDepth  : register( t7 ) < Attribute( "ShadowDepthBuffer" ); >;
#if defined( VULKAN )
	// Vulkan does not allow the same texture to be accessed with a comparison and non-comparison sampler
	Texture2D g_tShadowDepthBufferDepthNoCmp < Attribute( "ShadowDepthBufferNoCmp" );>;
#endif

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float CalculateDistanceFalloff( float flDistToLightSq, float4 vFalloffParams, float flMinDistance )
{
	flDistToLightSq = max( flDistToLightSq, flMinDistance );
	
	float2 vLightDistAndLightDistSq = float2( sqrt( flDistToLightSq ), flDistToLightSq );
	
	float flDot = dot( vLightDistAndLightDistSq, vFalloffParams.xy );
	
	return saturate( 1.0 / flDot - vFalloffParams.w );
}

//---------------------------------------------------------------------------------------------------------------------------------------------------------
float3 Position3WsToShadowTextureSpace(float3 vPositionWs, float4x4 matWorldToShadow)
{
    float4 vPositionTextureSpace = mul(float4(vPositionWs.xyz, 1.0), matWorldToShadow);
    return vPositionTextureSpace.xyz / vPositionTextureSpace.w;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
bool InsideShadowRegion(float3 vPositionTextureSpace, float4 vSpotLightShadowBounds)
{
    // Consider one texel from edge to be considered outside the shadow region so that multiple frusta won't do smooth sampling on edges
    const float flOneTexel = Shadow3x3PCFConstants[3].z; // 1.0 / 2048.0;

    float3 vInsideShadowRegion =
        step(float3(vSpotLightShadowBounds.xy + flOneTexel, 0.0), vPositionTextureSpace.xyz) *
        step(vPositionTextureSpace.xyz, float3(vSpotLightShadowBounds.zw - flOneTexel, 1.00f));
    return (vInsideShadowRegion.x * vInsideShadowRegion.y * vInsideShadowRegion.z) != 0.0f;
}

float DistanceToShadowEdge(float2 vPositionTextureSpace, float4 vSpotLightShadowBounds)
{
    // Spotlightbounds have the edges of the shadow region in the xy and zw components
    float2 vDistanceToShadowEdge = float2(0.0, 0.0);
    float2 scale = float2(vSpotLightShadowBounds.z - vSpotLightShadowBounds.x, vSpotLightShadowBounds.w - vSpotLightShadowBounds.y);
    vDistanceToShadowEdge.xy = max(vPositionTextureSpace.xy - vSpotLightShadowBounds.xy, vSpotLightShadowBounds.zw - vPositionTextureSpace.xy);
    return max(vDistanceToShadowEdge.x / scale.x, vDistanceToShadowEdge.y / scale.y);
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float ComputeShadow_ProjectiveDepthToLinearDepth(float flDepthPs, float4 vProjectiveDepthToLinearDepth)
{
    return
		mad(flDepthPs, vProjectiveDepthToLinearDepth.x, vProjectiveDepthToLinearDepth.y) /
           mad(flDepthPs, vProjectiveDepthToLinearDepth.z, vProjectiveDepthToLinearDepth.w);
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Gets the wanted shadow sampler given the context
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
Texture2D GetShadowSampler()
{
    return g_tShadowDepthBufferDepthNoCmp;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
float ComputeShadow_MinDepth_1x1(float3 vPositionTextureSpace)
{
    // Vulkan does not allow sampling a depth texture with both a comparison and non-comparison sampler.  So use a separate
    // texture in Vulkan for the regular depth sample
    return g_tShadowDepthBufferDepthNoCmp.SampleLevel( g_sShadowDepthSampler, vPositionTextureSpace.xy, 0.0 ).x;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Filter weights: 20 33 20
//                 33 55 33
//                 20 33 20
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Todo: Move all this shadow shit to Shadow.hlsl and use DPCF
float ComputeShadow_PCF_3x3_Gaussian(float3 vPositionTextureSpace)
{
    float2 shadowMapCenter = vPositionTextureSpace.xy;
    float objDepth = saturate(vPositionTextureSpace.z);

    float4 c0 = Shadow3x3PCFConstants[0].xyzw; // float4( 1.0 / 267.0, 7.0 / 267.0, 4.0 / 267.0, 20.0 / 267.0 );
    float4 c1 = Shadow3x3PCFConstants[1].xyzw; // float4( 33.0 / 267.0, 55.0 / 267.0, -flTexelEpsilon, 0.0 );
    float4 c2 = Shadow3x3PCFConstants[2].xyzw; // float4( flTwoTexelEpsilon, -flTwoTexelEpsilon, 0.0, flTexelEpsilon );
    float4 c3 = Shadow3x3PCFConstants[3].xyzw; // float4( flTexelEpsilon, -flTexelEpsilon, flTwoTexelEpsilon, -flTwoTexelEpsilon );

    float4 v20Taps;
    v20Taps.x = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c3.xx, objDepth ).x; //  1  1
    v20Taps.y = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c3.yx, objDepth ).x; // -1  1
    v20Taps.z = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c3.xy, objDepth ).x; //  1 -1
    v20Taps.w = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c3.yy, objDepth ).x; // -1 -1
    float flSum = dot(v20Taps.xyzw, float4(0.25, 0.25, 0.25, 0.25));
    if ((flSum == 0.0) || (flSum == 1.0))
        return flSum;
    flSum *= c0.w * 4.0;

    float4 v33Taps;
    v33Taps.x = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c2.wz, objDepth ).x; //  1  0
    v33Taps.y = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c1.zw, objDepth ).x; // -1  0
    v33Taps.z = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c1.wz, objDepth ).x; //  0 -1
    v33Taps.w = GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy + c2.zw, objDepth ).x; //  0  1
    flSum += dot(v33Taps.xyzw, c1.xxxx);

    flSum += GetShadowSampler().SampleCmpLevelZero( g_tShadowDepthBufferCmpSampler, shadowMapCenter.xy, objDepth ).x * c1.y;

    return flSum;
}

//---------------------------------------------------------------------------------------------------------------------------------------------------------
float ComputeShadow(float3 vPositionTextureSpace)
{
    return ComputeShadow_PCF_3x3_Gaussian(vPositionTextureSpace);
}

float ComputeShadow(float3 vPositionWs, float4x4 matWorldToShadow, float4 vSpotLightShadowBounds)
{
    const float3 vPositionTextureSpace = Position3WsToShadowTextureSpace(vPositionWs, matWorldToShadow);

    if (!InsideShadowRegion(vPositionTextureSpace, vSpotLightShadowBounds))
        return 1.0;

    return ComputeShadow(vPositionTextureSpace);
}
#endif // SHADOW_HLSL