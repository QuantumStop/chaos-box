//=========================================================================================================================
// Optional
//=========================================================================================================================
HEADER
{
	Description = "Foliage shader for s&box";
    DevShader = true;
    DebugInfo = false;
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
FEATURES
{
    #include "common/features.hlsl"
    Feature( F_ALPHA_TEST, 0..1, "Rendering" );
    Feature( F_FOLIAGE_ANIMATION, 0..1( 0 = "None", 1 = "Vertex Color Based" ), "Foliage Animation" );
    Feature( F_TRANSMISSIVE, 0..1, "Rendering" );
    Feature( F_TRANSMISSIVE_BACKFACE_NDOTL, 0..1, "Rendering" );
    FeatureRule( Requires1( F_TRANSMISSIVE_BACKFACE_NDOTL, F_TRANSMISSIVE ), "Requires transmissive" );    
}

//=========================================================================================================================
// Optional
//=========================================================================================================================
MODES
{
    Forward();													    // Indicates this shader will be used for main rendering
    Depth();
	ToolsShadingComplexity( "vr_tools_shading_complexity.shader" ); 	// Shows how expensive drawing is in debug view
}

//=========================================================================================================================
COMMON
{
    #include "common/shared.hlsl"
}

//=========================================================================================================================

struct VertexInput
{
	#include "common/vertexinput.hlsl"

    float4 vColor				: COLOR0 < Semantic( Color ); >;
};

//=========================================================================================================================

struct PixelInput
{
	#include "common/pixelinput.hlsl"

    float4 vColor				: COLOR0;
};

//=========================================================================================================================

VS
{
	#include "common/vertex.hlsl"

    StaticCombo( S_FOLIAGE_ANIMATION, F_FOLIAGE_ANIMATION, Sys( ALL ) );

    // Vertex Color
    #if S_FOLIAGE_ANIMATION == 1

    float g_flEdgeFrequency < Default( 0.33 ); Range( 0.0, 1.0 ); UiGroup( "Foliage Animation" ); >;
    float g_flEdgeAmplitude < Default( 0.3 ); Range( 0.0, 1.0 ); UiGroup( "Foliage Animation" ); >;
    float g_flBranchFrequency < Default( 0.33 ); Range( 0.0, 1.0 ); UiGroup( "Foliage Animation" ); >;
    float g_flBranchAmplitude < Default( 0.3 ); Range( 0.0, 1.0 ); UiGroup( "Foliage Animation" ); >;
    float g_flTrunkDeflection < Default( 0.0 ); Range( 0.0, 1.0 ); UiGroup( "Foliage Animation" ); >;
    float g_flTrunkDeflectionStart < Default( 0.0 ); Range( 0.0, 1000.0 ); UiGroup( "Foliage Animation" ); >;

    float4 SmoothCurve( float4 x )
    {  
        return x * x * ( 3.0 - 2.0 * x );  
    }  

    float4 TriangleWave( float4 x )
    {
        return abs( frac( x + 0.5 ) * 2.0 - 1.0 );  
    }  

    float4 SmoothTriangleWave( float4 x )
    {  
        return SmoothCurve( TriangleWave( x ) );  
    }

    // High-frequency displacement used in Unity's TerrainEngine; based on "Vegetation Procedural Animation and Shading in Crysis"
    // http://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch16.html
    void FoliageDetailBending( inout float3 vPositionOs, float3 vNormalOs, float3 vVertexColor, float3x4 matObjectToWorld, float3 vWind )
    {
        // 1.975, 0.793, 0.375, 0.193 are good frequencies   
        const float4 vFoliageFreqs = float4( 1.975, 0.793, 0.375, 0.193 );

        // Attenuation and phase offset is encoded in the vertex color
        const float flEdgeAtten = vVertexColor.r;
        const float flBranchAtten = vVertexColor.g;
        const float flDetailPhase = vVertexColor.b;

        // Material defined frequency and amplitude
        const float2 vTime = g_flTime.xx * float2( g_flEdgeFrequency, g_flBranchFrequency );
        const float flEdgeAmp = g_flEdgeAmplitude;
        const float flBranchAmp = g_flBranchAmplitude;

        // Phases
        float flObjPhase = dot( mul( matObjectToWorld, float4( 0, 0, 0, 1 ) ), 1 );
        float flBranchPhase = flDetailPhase + flObjPhase;
        float flVtxPhase = dot( vPositionOs.xyz, flDetailPhase + flBranchPhase );

        // fmod max phase avoid imprecision at high phases
        const float maxPhase = 50000.0f;

        // x is used for edges; y is used for branches
        float2 vWavesIn = ( vTime.xy + fmod( float2( flVtxPhase, flBranchPhase ), maxPhase ) ) * length( vWind );
        
        float4 vWaves = ( frac( vWavesIn.xxyy * vFoliageFreqs ) * 2.0 - 1.0 );
        vWaves = SmoothTriangleWave( vWaves );
        float2 vWavesSum = vWaves.xz + vWaves.yw;

        // Edge (xy) and branch bending (z)
        float flBranchWindBend = 1.0f - abs( dot( normalize( vWind.xyz ), normalize( float3( vPositionOs.xy, 0.0f ) ) ) );
        flBranchWindBend *= flBranchWindBend;

        vPositionOs.xyz += vWavesSum.x * flEdgeAtten * flEdgeAmp * vNormalOs.xyz;
        vPositionOs.xyz += vWavesSum.y * flBranchAtten * flBranchAmp * float3( 0.0f, 0.0f, 1.0f );
        vPositionOs.xyz += vWavesSum.y * flBranchAtten * flBranchAmp * flBranchWindBend * vWind.xyz;
    }

    // Main vegetation bending - displace verticies' xy positions along the wind direction
    // using normalized height to scale the amount of deformation.
    void FoliageMainBending( inout float3 vPositionOs, float3 vWind )
    {
        if ( g_flTrunkDeflection <= 0.0 ) return;

        float flLength = length( vPositionOs.xyz );
        // Bend factor - Wind variation is done on the CPU.  
        float flBF = 0.01f * max( vPositionOs.z - g_flTrunkDeflectionStart, 0 ) * g_flTrunkDeflection;  
        // Smooth bending factor and increase its nearby height limit.  
        flBF += 1.0f;
        flBF *= flBF;
        flBF = flBF * flBF - flBF;

        // Back and forth
        float flBend = pow( max( 0.0f, length( vWind ) - 1.0f ) / 4.0f, 2 ) * 4.0f;
        flBend = flBend + 0.7f * sqrt( flBend ) * sin( g_flTime );
        flBF *= flBend;

        // Displace position  
        float3 vNewPos = vPositionOs;
        vNewPos.xy += vWind.xy * flBF;

        // Rescale (reduces stretch)
        vPositionOs.xyz = normalize( vNewPos.xyz ) * flLength;
    }
#endif
	//
	// Main
	//
	PixelInput MainVs( VertexInput i )
	{
		PixelInput o = ProcessVertex( i );

        o.vColor = i.vColor;

        float3 vNormalOs;
        float4 vTangentUOs_flTangentVSign;

        VS_DecodeObjectSpaceNormalAndTangent( i, vNormalOs, vTangentUOs_flTangentVSign );

        float3 vPositionOs = i.vPositionOs.xyz;

        float3x4 matObjectToWorld = GetTransformMatrix( i.nInstanceTransformID );

#if ( S_FOLIAGE_ANIMATION == 1 )
        float3 vWind = g_vWindDirection.xyz * g_vWindStrengthFreqMulHighStrength.x;

        FoliageDetailBending( vPositionOs, vNormalOs, i.vColor.xyz, matObjectToWorld, vWind );
        FoliageMainBending( vPositionOs, vWind );
#endif

        o.vPositionWs = mul( matObjectToWorld, float4( vPositionOs.xyz, 1.0 ) );
	    o.vPositionPs.xyzw = Position3WsToPs( o.vPositionWs.xyz );

		// Add your vertex manipulation functions here
		return FinalizeVertex( o );
	}
}

//=========================================================================================================================

PS
{
    
    #include "common/utils/Material.CommonInputs.hlsl"
    #include "common/pixel.hlsl"
    
    StaticCombo( S_ALPHA_TEST, F_ALPHA_TEST, Sys( ALL ) );
    StaticCombo( S_TRANSMISSIVE, F_TRANSMISSIVE, Sys( ALL ) );
    StaticCombo( S_TRANSMISSIVE_BACKFACE_NDOTL, F_TRANSMISSIVE_BACKFACE_NDOTL, Sys( ALL ) );    

    #if S_ALPHA_TEST
        TextureAttribute( LightSim_Opacity_A, g_tColor );
    #endif

    #if S_TRANSMISSIVE
        CreateInputTexture2D( TextureTransmissiveColor, Srgb, 8, "", "_color", "Material,10/60", Default3( 1.0, 1.0, 1.0 ) );
        Texture2D g_tTransmissiveColor < Channel( RGB, Box( TextureTransmissiveColor ), Srgb );  OutputFormat( BC7 ); SrgbRead( true ); >;
    #endif

    RenderState( CullMode, F_RENDER_BACKFACES ? NONE : DEFAULT );

	//
	// Main
	//
	float4 MainPs( PixelInput i ) : SV_Target0
	{
        Material m = Material::From( i );

        #if ( S_TRANSMISSIVE )
            m.Transmission = g_tTransmissiveColor.Sample( TextureFiltering, i.vTextureCoords.xy ).rgb;
        #endif
        
	    return ShadingModelStandard::Shade( i, m );
	}
}