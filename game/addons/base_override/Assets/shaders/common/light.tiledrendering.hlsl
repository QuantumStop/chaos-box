#ifndef COMMON_PIXEL_LIGHTING_TILED_H
#define COMMON_PIXEL_LIGHTING_TILED_H

//-----------------------------------------------------------------------------

// Needed for lighting values
#include "common.fxc"

//-----------------------------------------------------------------------------

#if PROGRAM == VFX_PROGRAM_CS
    RWStructuredBuffer<uint> g_TiledLightBuffer < Attribute( "g_TiledLightBuffer" ); > ;
#else
    StructuredBuffer<uint> g_TiledLightBuffer < Attribute( "g_TiledLightBuffer" ); > ;
#endif

#define TILED_RENDERING_VERSION 1

int UseTiledRendering < Default( 1 ); Attribute( "UseTiledRendering" ); > ;

//-----------------------------------------------------------------------------

uint3 GetNumTiles()
{
    uint nNumMips = 5;
    uint2 nNumTiles = min(  ( ( uint2 )g_vViewportSize.xy ) >> 5, 128 );
    return uint3(  nNumTiles, nNumTiles.x * nNumTiles.y  ); // xy = num tiles, z = num tiles flattened
}

// -----------------------------------------------------------------------------

uint GetTileIdFlattened( uint2 tile )
{
    return tile.x + tile.y * GetNumTiles().x;
}

uint GetTileIdFlattenedEnvMap( uint2 tile )
{
    return GetTileIdFlattened( tile ) + ( GetNumTiles().z );
}

// -----------------------------------------------------------------------------

uint GetLightCountIndex( uint2 tile )
{
    return GetTileIdFlattened( tile );
}

uint GetCubeCountIndex( uint2 tile )
{
    return GetTileIdFlattenedEnvMap( tile );
}

uint GetLightStartOffset()
{
    return ( GetNumTiles().z ) * 2;
}

uint GetEnvMapStartOffset()
{
    return GetLightStartOffset() + ( GetNumTiles().z * MAX_LIGHTS_PER_TILE );
}

uint GetTileIdFlattenedDecal( uint2 tile )
{
    return GetTileIdFlattened( tile ) + GetEnvMapStartOffset() + ( GetNumTiles().z * MAX_ENVMAPS_PER_TILE );
}

uint GetDecalCountIndex( uint2 tile )
{
    return GetTileIdFlattenedDecal( tile );
}

uint GetDecalStartOffset()
{
    return GetEnvMapStartOffset() + ( GetNumTiles().z * MAX_ENVMAPS_PER_TILE ) + GetNumTiles().z;
}

uint GetLightOffsetForTile( uint2 tile )
{
    return GetLightStartOffset() + GetTileIdFlattened( tile ) * MAX_LIGHTS_PER_TILE;
}

uint GetEnvMapOffsetForTile( uint2 tile )
{
    return GetEnvMapStartOffset() + GetTileIdFlattened( tile ) * MAX_ENVMAPS_PER_TILE;
}

uint GetDecalOffsetForTile( uint2 tile )
{
    return GetDecalStartOffset() + GetTileIdFlattened( tile ) * MAX_DECALS_PER_TILE;
}

// -----------------------------------------------------------------------------

uint GetNumLightsPerTile( uint2 tile )
{
    uint tileIdxFlattened = GetLightCountIndex( tile );
    return min( g_TiledLightBuffer[tileIdxFlattened], MAX_LIGHTS_PER_TILE );
}

uint LoadLightByTile( uint2 tile, uint lightIndex )
{
    uint index = GetLightOffsetForTile( tile ) + lightIndex;
    return g_TiledLightBuffer[index];
}

// -----------------------------------------------------------------------------

uint GetNumEnvMapsPerTile( uint2 tile )
{
    uint tileIdxFlattened = GetCubeCountIndex( tile );
    return min( g_TiledLightBuffer[tileIdxFlattened], MAX_ENVMAPS_PER_TILE );
}

uint LoadEnvMapByTile( uint2 tile, uint envMapIndex )
{
    uint index = GetEnvMapOffsetForTile( tile ) + envMapIndex;
    return g_TiledLightBuffer[index];
}

uint GetNumDecalsPerTile( uint2 tile )
{
    uint tileIdxFlattened = GetDecalCountIndex( tile );
    return min( g_TiledLightBuffer[tileIdxFlattened], 32 );
}

uint LoadDecalByTile( uint2 tile, uint decalIndex )
{
    uint index = GetDecalOffsetForTile( tile ) + decalIndex;
    return g_TiledLightBuffer[index];
}

// -----------------------------------------------------------------------------
// Store functions for tiled rendering
// -----------------------------------------------------------------------------

#if PROGRAM == VFX_PROGRAM_CS
    void StoreLight( uint2 tile, uint lightID )
    {
        const uint nNumCurrentLights = GetNumLightsPerTile( tile );
        const uint tileLightIndex = GetLightOffsetForTile( tile ) + nNumCurrentLights;

        // Sign that this index has this lightID
        g_TiledLightBuffer[tileLightIndex] = lightID;

        // Increase the light count for this tile
        uint tileIdxFlattened = GetLightCountIndex( tile );
        g_TiledLightBuffer[tileIdxFlattened]++;
    }

    void StoreEnvMap( uint2 tile, uint cubeID )
    {
        const uint nNumCurrentCubes = GetNumEnvMapsPerTile( tile );
        const uint tileCubeIndex = GetEnvMapOffsetForTile( tile ) + nNumCurrentCubes;

        // Sign that this index has this cubeID
        g_TiledLightBuffer[tileCubeIndex] = cubeID;

        // Increase the cube count for this tile
        uint tileIdxFlattened = GetCubeCountIndex( tile );
        g_TiledLightBuffer[tileIdxFlattened]++;
    }

    void StoreDecal( uint2 tile, uint decalID )
    {
        const uint nNumCurrentDecals = GetNumDecalsPerTile( tile );
        const uint tileDecalIndex = GetDecalOffsetForTile( tile ) + nNumCurrentDecals;

        // Sign that this index has this cubeID
        g_TiledLightBuffer[tileDecalIndex] = decalID;

        // Increase the cube count for this tile
        uint tileIdxFlattened = GetDecalCountIndex( tile );
        g_TiledLightBuffer[tileIdxFlattened]++;
    }
#endif

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Tiled Rendering Helpers
//-------------------------------------------------------------------------------------------------------------------------------------------------------------

uint2 GetTileForScreenPosition( float2 vPositionSs )
{
    return min( ( ( uint2 )( vPositionSs.xy - g_vViewportOffset ) ) >> 5, GetNumTiles().xy - 1 );
}

uint GetNumLights( uint2 vTile )
{
    return UseTiledRendering == TILED_RENDERING_VERSION ? GetNumLightsPerTile( vTile ) : NumDynamicLights;
}

uint TranslateLightIndex( uint iLightIndex, uint2 vTile )
{
    return UseTiledRendering == TILED_RENDERING_VERSION ? LoadLightByTile( vTile, iLightIndex ) : iLightIndex;
}

uint GetNumEnvMaps( uint2 vTile )
{
    [branch]
    if ( UseTiledRendering == TILED_RENDERING_VERSION )
    {
        return GetNumEnvMapsPerTile( vTile );
    }
    else
    {
        return NumEnvironmentMaps;
    }
}

uint TranslateEnvMapIndex( uint iEnvMapIndex, uint2 vTile )
{
    [branch]
    if ( UseTiledRendering == TILED_RENDERING_VERSION )
    {
        return LoadEnvMapByTile( vTile, iEnvMapIndex );
    }
    else
    {
        return iEnvMapIndex;
    }
}

uint GetNumDecals( uint2 vTile )
{
    return GetNumDecalsPerTile( vTile );
}

uint TranslateDecalIndex( uint iDecalIndex, uint2 vTile )
{
    return LoadDecalByTile( vTile, iDecalIndex );
}

#endif // COMMON_PIXEL_LIGHTING_TILED_H