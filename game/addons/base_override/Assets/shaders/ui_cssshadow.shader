HEADER
{
	DevShader = true;
	Version = 1;
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
MODES
{
	Default();
	Forward();
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
FEATURES
{
	#include "ui/features.hlsl"
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
COMMON
{
	#include "ui/common.hlsl"
}
  
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
VS
{
	#include "ui/vertex.hlsl"  
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
PS
{
	#include "ui/pixel.hlsl"  

	// Texture Samplers ---------------------------------------------------------------------------------------------------------------------------------------
	CreateInputTexture2D( Texture, Srgb, 8, "", "", "Color", Default3( 1.0, 1.0, 1.0 ) );
	Texture2D g_tColor < Channel( RGBA, None( Texture ), Srgb ); OutputFormat( DXT5 ); SrgbRead( true ); >;
	TextureAttribute( RepresentativeTexture, g_tColor );

	float4 CornerRadius < Attribute( "BorderRadius" ); >;
	float ShadowWidth < UiGroup( "Shadow" ); Attribute( "ShadowWidth" ); >;
	float Bloat < Attribute( "Bloat" ); >;
	float2 ShadowOffset < Attribute( "ShadowOffset" ); >;
	bool Inset < Attribute ( "Inset" ); >;

	// Render State -------------------------------------------------------------------------------------------------------------------------------------------
	RenderState( SrgbWriteEnable0, true );

	// Always write rgba
	RenderState( ColorWriteEnable0, RGBA );
	RenderState( FillMode, SOLID );

	// Never cull
	RenderState( CullMode, NONE );

	// No depth
	RenderState( DepthWriteEnable, false );

	// Main ---------------------------------------------------------------------------------------------------------------------------------------------------

	float RoundedRectangle( float2 pos, float2 center, float2 box, float size )
	{
		return size - length( pos - center );
	}

	float DrawCurvedRectWithBorder( float2 pos, float2 size )
	{
		float f = 1; // our panel alpha - we'll set to 0 for outside of radius corners

		f = min( pos.x, size.x - pos.x );
		f = min( f, pos.y );
		f = min( f, size.y - pos.y );
		
		float radAdd = ShadowWidth * 0.4;

		// Inset shadows actually need to subtract here
		if ( Inset )
			radAdd = -radAdd;

		//
		// Top Left Radius
		//
		float r = min( size.y * 0.5, CornerRadius[0] + radAdd );
		if ( pos.x < r && pos.y < r )
		{
			f = min( f, RoundedRectangle( pos, r, size, r ) );
		}

		//
		// Top Right Radius
		//
		r = min( size.y * 0.5, CornerRadius[1] + radAdd );
		if ( pos.x > size.x - r && pos.y < r )
		{
			f = min( f, RoundedRectangle( pos, float2( size.x - r, r ), size, r ) );
		}

		//
		// Bottom Right Radius
		//
		r = min( size.y * 0.5, CornerRadius[3] + radAdd);
		if ( pos.x > size.x - r && pos.y > size.y - r )
		{
			f = min( f, RoundedRectangle( pos, float2( size.x - r, size.y - r ), size, r ) );
		}

		//
		// Right Bottom Radius
		//
		r = min( size.y * 0.5, CornerRadius[2] + radAdd );
		if ( pos.x < r && pos.y > size.y - r )
		{
			f = min( f, RoundedRectangle( pos, float2( r, size.y - r ), size, r ) );
		}

		return f;
	}

	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;
		UI_CommonProcessing_Pre( i );
		
		float2 pos = (BoxSize + float2(Bloat, Bloat) * 2) * i.vTexCoord.xy - float2(Bloat, Bloat);
		
		// Inset shadows are drawn in the opposite direction
		if ( Inset )
			pos -= ShadowOffset;

		float distanceFromEdge = DrawCurvedRectWithBorder( pos, BoxSize ); 

		distanceFromEdge = smoothstep( -ShadowWidth * 0.5, ShadowWidth * 0.5, distanceFromEdge );
		distanceFromEdge = saturate( distanceFromEdge );

		// Inset shadows are drawn 'backwards', so we need to invert the distance
		if ( Inset )
			distanceFromEdge = 1.0 - distanceFromEdge;

		o.vColor = i.vColor;
		o.vColor.a *= distanceFromEdge;

		return UI_CommonProcessing_Post( i, o );
	}
}
