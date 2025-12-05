#include "common.fxc"
#include "math_general.fxc"
#include "ui/scissor.hlsl"
#include "common/blendmode.hlsl"

// Defines ------------------------------------------------------------------------------------------------------------------------------------------------

#define SUBPIXEL_AA_MAGIC 0.5

// Attributes ---------------------------------------------------------------------------------------------------------------------------------------------
float2 BoxSize < Attribute( "BoxSize" ); >;
float2 BoxPosition < Attribute( "BoxPosition" ); >;

// Main ---------------------------------------------------------------------------------------------------------------------------------------------------
struct PS_OUTPUT
{
    float4 vColor : SV_Target0;
};

void UI_CommonProcessing_Pre( PS_INPUT i )
{
    if ( HasScissoring )
    {
        SoftwareScissoring( i );
    }
}

PS_OUTPUT UI_CommonProcessing_Post( PS_INPUT i, PS_OUTPUT o )
{
    return o;
}


#if ( D_NO_ZTEST )
    RenderState( DepthEnable, false );
#else
    RenderState( DepthEnable, true );
#endif