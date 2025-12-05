HEADER
{
	DevShader = true;
	Description = "A";
}

MODES
{
	Default();
}

FEATURES
{
}

COMMON
{
	#include "system.fxc" // This should always be the first include in COMMON
}

CS
{
	#include "common.fxc"

    RWTexture2D<float4> ControlMap < Attribute( "ControlMap" ); >;
    float2 ControlUV < Attribute( "ControlUV" ); >;
    int BrushSize < Attribute( "BrushSize" ); >;
    float BrushStrength < Attribute( "BrushStrength" ); >;
	Texture2D<float> Brush < Attribute( "Brush" ); >;
    int SplatChannel < Attribute( "SplatChannel" ); >;

    SamplerState g_sBilinearBorder < Filter( BILINEAR ); AddressU( BORDER ); AddressV( BORDER ); >;
    
    void BalanceVector4Weights( float4 weights, out float4 balancedWeights )
    {
        float totalInfluence = weights.x + weights.y + weights.z + weights.w;
    
        // If no weights exist, force the first channel
        if ( totalInfluence == 0 )
            balancedWeights = float4( 1, 0, 0, 0 );
        else
            balancedWeights = weights * ( 1 / totalInfluence );
    }

	[numthreads( 16, 16, 1 )]
	void MainCs( uint nGroupIndex : SV_GroupIndex, uint3 vThreadId : SV_DispatchThreadID )
	{
        float w, h;
        ControlMap.GetDimensions( w, h );

        int2 texelCenter = int2( float2( w, h ) * ControlUV );
        int2 texelOffset = int2( vThreadId.xy ) - int( BrushSize / 2 );

        int2 texel = texelCenter + texelOffset;
        if ( texel.x < 0 || texel.y < 0 || texel.x >= w || texel.y >= h ) return;

        float2 brushUV = float2( vThreadId.xy ) / BrushSize;
        float brush = Brush.SampleLevel( g_sBilinearBorder, brushUV, 0 ) * BrushStrength;

        float4 height = ControlMap.Load( texel );

        [branch] if( SplatChannel == 0 )
            height.r += brush;
        else if( SplatChannel == 1 )
            height.g += brush;
        else if( SplatChannel == 2 )
            height.b += brush;
        else if( SplatChannel == 3 )
            height.a += brush;

        float4 balanced;
        BalanceVector4Weights( height, balanced );

        ControlMap[texel] = balanced;
        // ControlMap[texel] = float4( 0, 0, 1, 0 );
    }
}

