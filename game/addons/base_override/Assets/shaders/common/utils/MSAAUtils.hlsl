#ifndef MSAA_UTILS_HLSL
#define MSAA_UTILS_HLSL

#include "common/classes/Depth.hlsl"
class MSAAUtils
{
    // Gets the correct sample index on the lane for the given pixel position and UV coordinates.
    // This is used to composite a non-MSAA texture in a MSAA buffer.
    // You can use either QuadReadLaneAt or Texture2D::GatherRed to get the correct sample.
    static int GetSampleIndex( float4 vPositionSs, float2 uv )
    {
        float4 v4Depths = g_tDepthChain.GatherRed( g_sBilinearClamp, uv.xy );
        float4 vDepthDiffs = abs( vPositionSs.zzzz - v4Depths.xyzw );

        // Find the minimum depth difference
        float minDepthDiff = min(min(vDepthDiffs.x, vDepthDiffs.y), min(vDepthDiffs.z, vDepthDiffs.w));

        // Explicit vector comparison to find the closest actual index.
        int4 indices = int4(0, 1, 2, 3);
        int4 matchIndices = indices * (vDepthDiffs == minDepthDiff);
        int selectedIndex = max(max(matchIndices.x, matchIndices.y), max(matchIndices.z, matchIndices.w));

        // Return the index value for the first matching
        return selectedIndex;
    }
};

#endif // MSAA_UTILS_HLSL