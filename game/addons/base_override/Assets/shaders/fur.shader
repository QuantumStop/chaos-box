
HEADER
{
	Description = "";
}

FEATURES
{
	#include "common/features.hlsl"
}

MODES
{
	Forward();
	Depth(); 
	ToolsShadingComplexity( "tools_shading_complexity.shader" );
}

COMMON
{
	#ifndef S_ALPHA_TEST
	#define S_ALPHA_TEST 1
	#endif
	#ifndef S_TRANSLUCENT
	#define S_TRANSLUCENT 0
	#endif
	
	#include "common/shared.hlsl"
	#include "procedural.hlsl"

	#define S_UV2 1
}

struct VertexInput
{
	#include "common/vertexinput.hlsl"
	float4 vColor : COLOR0 < Semantic( Color ); >;
};

struct PixelInput
{
	#include "common/pixelinput.hlsl"
	float3 vPositionOs : TEXCOORD14;
	float3 vNormalOs : TEXCOORD15;
	float4 vTangentUOs_flTangentVSign : TANGENT	< Semantic( TangentU_SignV ); >;
	float4 vColor : COLOR0;
	float4 vTintColor : COLOR1;
};

VS
{
	#include "common/vertex.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( FurNoise, Linear, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tFurNoise < Channel( RGBA, Box( FurNoise ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	float g_flNoiseTiling < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 64 ); >;
	float g_flWindFreq < UiGroup( ",0/,0/0" ); Default1( 0.36574125 ); Range1( 0, 100 ); >;
	float g_flWindNoise < UiGroup( ",0/,0/0" ); Default1( 0.36574125 ); Range1( 0, 10 ); >;
	float g_flWind < UiGroup( ",0/,0/0" ); Default1( 0.36574125 ); Range1( 0, 100 ); >;
	
	PixelInput MainVs( VertexInput v )
	{
		PixelInput i = ProcessVertex( v );
		i.vPositionOs = v.vPositionOs.xyz;
		i.vColor = v.vColor;

		ExtraShaderData_t extraShaderData = GetExtraPerInstanceShaderData( v.nInstanceTransformID );
		i.vTintColor = extraShaderData.vTint;

		VS_DecodeObjectSpaceNormalAndTangent( v, i.vNormalOs, i.vTangentUOs_flTangentVSign );
		
		float l_0 = g_flNoiseTiling;
		float2 l_1 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_0, l_0 ), float2( 0, 0 ) );
		float4 l_2 = g_tFurNoise.SampleLevel( g_sSampler0, l_1, 0 );
		float l_3 = g_flWindFreq;
		float l_4 = g_flWindNoise;
		float l_5 = g_flTime * l_4;
		float2 l_6 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_3, l_3 ), float2( l_5, l_5 ) );
		float l_7 = Simplex2D( l_6 );
		float l_8 = g_flWind;
		float l_9 = l_7 * l_8;
		float l_10 = l_2.r * l_9;
		i.vPositionWs.xyz += float3( l_10, l_10, l_10 );
		i.vPositionPs.xyzw = Position3WsToPs( i.vPositionWs.xyz );
		
		return FinalizeVertex( i );
	}
}

PS
{
	#include "common/pixel.hlsl"
	
	SamplerState g_sSampler0 < Filter( ANISO ); AddressU( WRAP ); AddressV( WRAP ); >;
	CreateInputTexture2D( FurNoise, Linear, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( BaseColor, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Normal, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Roughness, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( Metalness, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	CreateInputTexture2D( AO, Srgb, 8, "None", "_color", ",0/,0/0", Default4( 1.00, 1.00, 1.00, 1.00 ) );
	Texture2D g_tFurNoise < Channel( RGBA, Box( FurNoise ), Linear ); OutputFormat( DXT5 ); SrgbRead( False ); >;
	Texture2D g_tBaseColor < Channel( RGBA, Box( BaseColor ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tNormal < Channel( RGBA, Box( Normal ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tRoughness < Channel( RGBA, Box( Roughness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tMetalness < Channel( RGBA, Box( Metalness ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	Texture2D g_tAO < Channel( RGBA, Box( AO ), Srgb ); OutputFormat( DXT5 ); SrgbRead( True ); >;
	float4 g_vRimColour < UiType( Color ); UiGroup( ",0/,0/0" ); Default4( 1.00, 1.00, 1.00, 1.00 ); >;
	float g_flRimPower < UiGroup( ",0/,0/0" ); Default1( 0.8429716 ); Range1( 0, 10 ); >;
	float g_flRimFudge < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flNoiseTiling < UiGroup( ",0/,0/0" ); Default1( 1 ); Range1( 0, 64 ); >;
	float g_flNoiseAlbedoMultiply < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flMinClipFudge < UiGroup( ",0/,0/0" ); Default1( 0.01 ); Range1( 0, 1 ); >;
	float g_flAOAmount < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	float g_flNoiseAOAmount < UiGroup( ",0/,0/0" ); Default1( 0 ); Range1( 0, 1 ); >;
	
	float4 MainPs( PixelInput i ) : SV_Target0
	{
		Material m = Material::Init();
		m.Albedo = float3( 1, 1, 1 );
		m.Normal = float3( 0, 0, 1 );
		m.Roughness = 1;
		m.Metalness = 0;
		m.AmbientOcclusion = 1;
		m.TintMask = 1;
		m.Opacity = 1;
		m.Emission = float3( 0, 0, 0 );
		m.Transmission = 0;
		
		float4 l_0 = g_vRimColour;
		float l_1 = g_flRimPower;
		float3 l_2 = CalculatePositionToCameraDirWs( i.vPositionWithOffsetWs.xyz + g_vHighPrecisionLightingOffsetWs.xyz );
		float3 l_3 = pow( 1.0 - dot( normalize( i.vNormalWs ), normalize( l_2 ) ), l_1 );
		float l_4 = g_flRimFudge;
		float3 l_5 = saturate( ( l_3 - float3( 0, 0, 0 ) ) / ( float3( l_4, l_4, l_4 ) - float3( 0, 0, 0 ) ) ) * ( float3( 1, 1, 1 ) - float3( 0, 0, 0 ) ) + float3( 0, 0, 0 );
		float4 l_6 = l_0 * float4( l_5, 0 );
		float l_7 = g_flNoiseTiling;
		float2 l_8 = TileAndOffsetUv( i.vTextureCoords.xy, float2( l_7, l_7 ), float2( 0, 0 ) );
		float4 l_9 = Tex2DS( g_tFurNoise, g_sSampler0, l_8 );
		float l_10 = g_flNoiseAlbedoMultiply;
		float l_11 = lerp( 1, l_9.r, l_10 );
		float4 l_12 = Tex2DS( g_tBaseColor, g_sSampler0, i.vTextureCoords.xy );
		float4 l_13 = float4( l_11, l_11, l_11, l_11 ) * l_12;
		float4 l_14 = l_6 + l_13;
		float l_15 = g_flMinClipFudge;
		float l_16 = saturate( ( l_9.r - 0 ) / ( 1 - 0 ) ) * ( 1 - l_15 ) + l_15;
		float3 l_17 = i.vColor.rgb;
		float3 l_18 = float3( l_16, l_16, l_16 ) - l_17;
		float3 l_19 = l_18 + float3( 0.5, 0.5, 0.5 );
		float4 l_20 = Tex2DS( g_tNormal, g_sSampler0, i.vTextureCoords.xy );
		float4 l_21 = Tex2DS( g_tRoughness, g_sSampler0, i.vTextureCoords.xy );
		float4 l_22 = Tex2DS( g_tMetalness, g_sSampler0, i.vTextureCoords.xy );
		float4 l_23 = Tex2DS( g_tAO, g_sSampler0, i.vTextureCoords.xy );
		float l_24 = g_flAOAmount;
		float4 l_25 = lerp( float4( 1, 1, 1, 1 ), l_23, l_24 );
		float l_26 = g_flNoiseAOAmount;
		float l_27 = lerp( 1, l_9.r, l_26 );
		float4 l_28 = l_25 * float4( l_27, l_27, l_27, l_27 );
		
		m.Albedo = l_14.xyz;
		m.Opacity = l_19.x;
		m.Normal = l_20.xyz;
		m.Roughness = l_21.x;
		m.Metalness = l_22.x;
		m.AmbientOcclusion = l_28.x;
		
		m.AmbientOcclusion = saturate( m.AmbientOcclusion );
		m.Roughness = saturate( m.Roughness );
		m.Metalness = saturate( m.Metalness );
		m.Opacity = saturate( m.Opacity );

		// Result node takes normal as tangent space, convert it to world space now
		m.Normal = TransformNormal( m.Normal, i.vNormalWs, i.vTangentUWs, i.vTangentVWs );

		// for some toolvis shit
		m.WorldTangentU = i.vTangentUWs;
		m.WorldTangentV = i.vTangentVWs;
        m.TextureCoords = i.vTextureCoords.xy;
		
		return ShadingModelStandard::Shade( i, m );
	}
}
