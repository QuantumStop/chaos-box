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
	
	DynamicCombo( D_LAYERED, 0..1, Sys( PC ) );
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

	float4 CornerRadius < Attribute( "BorderRadius" ); >;

	float Brightness < Attribute( "Brightness" ); Default( 1 ); >;
	float Contrast < Attribute( "Contrast" );  Default( 1 ); >;
	float Saturate < Attribute( "Saturate" );  Default( 1 ); >;
	float Invert < Attribute( "Invert" );  Default( 0 ); >;
	float HueRotate < Attribute( "HueRotate" );  Default( 0 ); >;
	float Sepia< Attribute("Sepia"); Default( 0 ); >;
	float BlurScale < Attribute( "BlurScale" ); Default( 10 ); >;

	BoolAttribute( bWantsFBCopyTexture, true );
	Texture2D g_tFrameBufferCopyTexture < Attribute( "FrameBufferCopyTexture" ); SrgbRead( true ); >;
	float4 g_vFBCopyTextureRect < Attribute( "FrameBufferCopyRectangle" ); Default4( 0., 0., 1.0, 1.0 ); >;

	float4 g_vViewport < Source( Viewport ); >; 

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

	float GetDistanceFromEdge(float2 pos, float2 size, float4 cornerRadius)
	{
		float minCorner = min(size.x, size.y);

			//Based off https://iquilezles.org/www/articles/distfunctions2d/distfunctions2d.htm

		float4 r = min(cornerRadius * 2.0, minCorner);
		r.xy = (pos.x > 0.0) ? r.xy : r.zw;
		r.x = (pos.y > 0.0) ? r.x : r.y;
		float2 q = abs(pos) - (size) + r.x;
		return -1.0 + min(max(q.x, q.y), 0.0) + length(max(q, 0.0)) - r.x;
	}

	float4 DoColorMatrix( float4 color, float4x4 mColorMatrix )
	{
		return saturate(mul(mColorMatrix, color));
	}

	float3 DoColorMatrix( float3 color, float4x4 mColorMatrix )
	{
		return mul(mColorMatrix, float4( color, 1.0f )).rgb;
	}

	float4 DoBackdropFilter( float2 uv )
	{
		// transform the uv by the g_vFBCopyTextureRect
		uv.x = uv.x * g_vFBCopyTextureRect.z;
		uv.y = uv.y * g_vFBCopyTextureRect.w;

		float3 backdrop = g_tFrameBufferCopyTexture.SampleLevel( g_sTrilinearClamp, uv, sqrt( BlurScale / 2 ) ).rgb;

		backdrop = SrgbLinearToGamma( backdrop );

		// Sepia
		backdrop = DoColorMatrix (
			backdrop, 
			float4x4(
				0.393f + 0.607f * (1.0f - Sepia), 0.769f - 0.769f * (1.0f - Sepia), 0.189f - 0.189f * (1.0f - Sepia), 0.0f,
				0.349f - 0.349f * (1.0f - Sepia), 0.686f + 0.314f * (1.0f - Sepia), 0.168f - 0.168f * (1.0f - Sepia), 0.0f,
				0.272f - 0.272f * (1.0f - Sepia), 0.534f - 0.534f * (1.0f - Sepia), 0.131f + 0.869f * (1.0f - Sepia), 0.0f,
				0.0f, 0.0f, 0.0f, 1.0f
			)
		);

		// invert ( default 0 )
		backdrop = lerp(backdrop, 1 - backdrop, Invert);

		 // Contrast (default 1)
		backdrop = saturate(lerp(float3(0.5, 0.5, 0.5), backdrop, Contrast));

		backdrop = SrgbGammaToLinear( backdrop );

		float3 hsv = RgbToHsv( backdrop );
		hsv.r += (HueRotate / 360); // param to normalized degrees
		hsv.r = hsv.r % 1;
		hsv.g = lerp( 0, hsv.g, Saturate ); // saturation
		hsv.b *= Brightness; // value
		
		backdrop = HsvToRgb( hsv );

		return float4( backdrop, 1 );
	}


	PS_OUTPUT MainPs( PS_INPUT i )
	{
		PS_OUTPUT o;
		
		UI_CommonProcessing_Pre( i );

#if D_WORLDPANEL
		float2 vUV = (i.vPositionPs.xy - g_vViewportOffset) * g_vInvViewportSize;
#else
		#if D_LAYERED
				float2 vUV = ((BoxPosition + i.vPositionPs.xy - g_vViewportOffset) * g_vInvViewportSize);
		#else
				float2 vUV = i.vTexCoord.zw;
		#endif
#endif

		o.vColor = DoBackdropFilter( vUV );

		float2 pos = ( BoxSize ) * (i.vTexCoord.xy * 2.0 - 1.0);
		float dist = GetDistanceFromEdge(pos, BoxSize, CornerRadius);
		o.vColor.a = saturate( -dist * SUBPIXEL_AA_MAGIC ) * i.vColor.a;

		return UI_CommonProcessing_Post( i, o );
	}
}
