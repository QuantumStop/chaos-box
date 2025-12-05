#ifndef AMBIENTOCCLUSION_HLSL
#define AMBIENTOCCLUSION_HLSL

#include "common/utils/MSAAUtils.hlsl"

// Ambient occlusion texture index for bindless access
int ScreenSpaceOcclusionTexture < Attribute( "ScreenSpaceAmbientOcclusionTexture" ); Default( 0 ); >;

class ScreenSpaceAmbientOcclusion
{
    // Samples ambient occlusion texture at the given screen position
    // Does depth comparison to find the best sample in MSAA
    static float Sample( float4 ScreenPosition )
    {
        if ( ScreenSpaceOcclusionTexture == 0 )
            return 1.0f; // Ambient occlusion is disabled

        float2 uv = ScreenPosition.xy * g_vInvViewportSize.xy;
        uv -= 0.5f * g_vInvViewportSize.xy; // Offset by half a pixel so it matches the center pixel
        
        Texture2D tAO = Bindless::GetTexture2D( ScreenSpaceOcclusionTexture );

        // Get the correct quad index so we can composite with MSAA on a non-MSAA texture
        int nQuadIndex = MSAAUtils::GetSampleIndex( ScreenPosition, uv );
        return tAO.GatherRed( g_sBilinearClamp, uv.xy )[nQuadIndex];
    }
};

#endif //AMBIENTOCCLUSION_HLSL