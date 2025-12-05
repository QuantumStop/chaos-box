//-------------------------------------------------------------------------------------------------------------------------------------------------------------
HEADER
{
	DevShader = true;
	Description = "Compute Shader for processing light culling by tiles.";
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
MODES
{
	Default();
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
FEATURES
{
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
COMMON
{
	#include "system.fxc" // This should always be the first include in COMMON
    #include "vr_common.fxc"
    
    enum CullingLightJobs
    {
        CULLING_LIGHT_JOB_CULL_LIGHTS, 		// Cull and test visibility dynamic lights
        CULLING_LIGHT_JOB_STATIC_LIGHTS, 	// Test Visibility For Static Lights
        CULLING_LIGHT_JOB_CULL_ENVMAPS, 	// Cull envmaps
        CULLING_LIGHT_JOB_COUNT
    }

    DynamicCombo( D_CONSERVATIVE_CULLING,          0..1, Sys( ALL ) );
}

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
CS
{
    #define MINIMAL_MATERIAL

    // -------------------------------------------------------------------------------------------------------------------------------------------------------------
    #include "vr_lighting.fxc"
    #include "common/lightbinner.hlsl"
    #include "tiled_culling.hlsl"
    #include "common/classes/Depth.hlsl"
    #include "common/classes/Decals.hlsl"

	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
	//
	// System Parameters 
	//
	//-------------------------------------------------------------------------------------------------------------------------------------------------------------
    Texture2D DepthChainDownsample < Attribute( "DepthChainDownsample" ); SrgbRead( false ); >;

    // -------------------------------------------------------------------------------------------------------------------------------------------------------------

    float2 FetchDepthMinMax( uint2 vPositionSs )
    {
        // Calculate depth
        float2 vProjectedDepth = DepthChainDownsample.Load( int3( vPositionSs.xy, 5 ) ).xy;

        // Convert to view space
        return float2(  Depth::Linearize( Depth::Normalize( vProjectedDepth.y ), vPositionSs.xy * CULLING_TILE_SIZE ), 
                        Depth::Linearize( Depth::Normalize( vProjectedDepth.x ), vPositionSs.xy * CULLING_TILE_SIZE ) );
    }
    
    // -------------------------------------------------------------------------------------------------------------------------------------------------------------

    void SortLights( uint2 vTile, Frustum screenFrustum, const int2 range, const bool bStore )
    {
        // Reset light count
        uint tileIdxFlattenedLight = GetTileIdFlattened( vTile );

        if( bStore )
            g_TiledLightBuffer[ tileIdxFlattenedLight ] = 0; // Reset light count
        
        uint nCount = 0;

        // check every light
        for ( int i = range.x; i < range.y && nCount < MAX_LIGHTS_PER_TILE; i++ )
        {
            const BinnedLight light = BinnedLightBuffer[i];

            bool bHit = false;

            if( light.IsSpotLight() )
                bHit = screenFrustum.ConeInside( light.GetPosition(), light.GetDirection(), light.GetDirectionUp(), light.GetRadius(), light.SpotLightInnerOuterConeCosines.y );
            else
                bHit = screenFrustum.SphereInside( light.GetPosition(), light.GetRadius() );

            if( bHit )
            {
                if( bStore )
                    StoreLight( vTile, i );

                nCount++;
            }
        }
    }

    void SortEnvMaps( uint2 vTile, Frustum screenFrustum )
    {
        // Reset envmap count
        uint tileIdxFlattenedCube = GetTileIdFlattenedEnvMap( vTile );
        g_TiledLightBuffer[ tileIdxFlattenedCube ] = 0; // Reset envmap count
        uint nCount = 0;

        for( int nEnvMap=0; nEnvMap < NumEnvironmentMaps && nCount < MAX_ENVMAPS_PER_TILE; nEnvMap++ )
        {
			const float flEdgeFeathering = max( EnvMapFeathering( nEnvMap ), 0.0f );

			const float3 vEnvMapMin = EnvMapBoxMins(nEnvMap) - flEdgeFeathering;
			const float3 vEnvMapMax = EnvMapBoxMaxs(nEnvMap) + flEdgeFeathering;

            bool bHit = screenFrustum.AABBInside( vEnvMapMin, vEnvMapMax, EnvMapWorldToLocal( nEnvMap ) );

            if( bHit > 0 )
            {
                StoreEnvMap( vTile, nEnvMap );
                nCount++;
            }
        }
    }

    bool PointOutsidePlane( float3 p, Plane plane )
    {
        return dot( plane.Normal, p ) - plane.Distance < 0;
    }

    // Modified version of DecalOutsidePlane using direct vector rotation
    bool DecalOutsidePlane(Decal decal, Plane plane)
    {
        float3 decal_world_center = -decal.WorldPosition.xyz;
        
        // Conjugate quat to invert it to local to world
        float4 quat_decal_to_world = float4(-decal.Quat.xyz, decal.Quat.w);

        // Get decal's local axes rotated into world space
        float3 decal_axis_x_world = RotateVector(float3(1.0f, 0.0f, 0.0f), quat_decal_to_world);
        float3 decal_axis_y_world = RotateVector(float3(0.0f, 1.0f, 0.0f), quat_decal_to_world);
        float3 decal_axis_z_world = RotateVector(float3(0.0f, 0.0f, 1.0f), quat_decal_to_world);

        float3 half_extents_vectors_world[3];
        half_extents_vectors_world[0] = decal_axis_x_world * (0.5f / decal.Scale.x);
        half_extents_vectors_world[1] = decal_axis_y_world * (0.5f / decal.Scale.y);
        half_extents_vectors_world[2] = decal_axis_z_world * (0.5f / decal.Scale.z);

        // Projection of decal's center onto plane normal, relative to plane
        float dist_center_to_plane = dot(plane.Normal, decal_world_center) - plane.Distance;

        // Projection radius of OBB onto plane normal
        float radius = abs(dot(plane.Normal, half_extents_vectors_world[0])) + 
                    abs(dot(plane.Normal, half_extents_vectors_world[1])) +
                    abs(dot(plane.Normal, half_extents_vectors_world[2]));

        return dist_center_to_plane < -radius;
    }

    bool DecalInsideFrustum( Decal decal, Frustum frustum )
    {
        if ( DecalOutsidePlane( decal, frustum.Planes[5] ) || DecalOutsidePlane( decal, frustum.Planes[4] ) )
        {
            return false;
        }

        for ( int i = 0; i < 4; i++ )
        {		
            if ( DecalOutsidePlane(decal, frustum.Planes[i]) )
            {
                return false;
            }
        }	

        return true;
    }

    void CullDecals( uint2 vTile, Frustum screenFrustum )
    {
        uint tileIdxFlattenedCube = GetTileIdFlattenedDecal( vTile );
        g_TiledLightBuffer[ tileIdxFlattenedCube ] = 0; // Reset decal count

        for( int nDecal=0; nDecal < NumDecals; nDecal++ )
        {
            Decal decal = DecalBuffer[nDecal];

            if ( DecalInsideFrustum( decal, screenFrustum ) )
            {
                StoreDecal( vTile, nDecal );
            }
        }
    }

	[numthreads( 8, 8, 3)]
	void MainCs( uint nGroupIndex : SV_GroupIndex, uint3 vThreadId : SV_DispatchThreadID )
	{
        const uint2 vTile = vThreadId.xy;
        const uint vJobId = vThreadId.z;

        if( vTile.x >= GetNumTiles().x || vTile.y >= GetNumTiles().y )
            return;

        const bool bDepthCullNearZ = D_CONSERVATIVE_CULLING == 1;
        Frustum screenFrustum = Frustum::CalculateScreenTiles( vTile.xy, FetchDepthMinMax( vTile.xy ), bDepthCullNearZ );

        // matt: why are all these methods named sort, they're culling?

        // ----------------------------------------------------------------------------------------------------------------------
        [branch]
        if( vJobId == CULLING_LIGHT_JOB_CULL_LIGHTS )
        {
            SortLights( vTile, screenFrustum, int2( 0, NumDynamicLights ), true );
        }
        else if ( vJobId == CULLING_LIGHT_JOB_STATIC_LIGHTS )
        {
            // Visibility of static lights exclusively, don't store it on tiled buffer
            SortLights( vTile, screenFrustum, int2( NumDynamicLights, NumDynamicLights + NumBakedIndexedLights ), false );
        }
        else if ( vJobId == CULLING_LIGHT_JOB_CULL_ENVMAPS )
        {
            SortEnvMaps( vTile, screenFrustum );

            // stealing your thread
            CullDecals( vTile, screenFrustum );
        }
        // ----------------------------------------------------------------------------------------------------------------------
        
    }
}

