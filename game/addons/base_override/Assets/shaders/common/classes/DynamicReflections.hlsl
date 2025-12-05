#ifndef DYNAMIC_REFLECTIONS_HLSL
#define DYNAMIC_REFLECTIONS_HLSL

//-------------------------------------------------------------------------------------------------------------------------------------------------------------

int ReflectionColorIndex < Attribute("ReflectionColorIndex" ); Default(-1); >;

//-------------------------------------------------------------------------------------------------------------------------------------------------------------
// Accessor for the result of dynamic reflections, whether they are SSR or eventually Raytraced Reflections
//-------------------------------------------------------------------------------------------------------------------------------------------------------------
class DynamicReflections
{
    static float4 Sample(float2 ScreenPosition, float Roughness = 0.0f)
    {
        if (!IsEnabled())
            return 0;
    
        Texture2D ReflectionColor = Bindless::GetTexture2D( ReflectionColorIndex );

        // If the texture has mips, we can sample it at a specific level based on roughness.
        // Eg Planar Reflections with mip chain.
        int2 nDim;
        int nLevels;
        ReflectionColor.GetDimensions(0, nDim.x, nDim.y, nLevels);

        float flLevel = ( Roughness * (nLevels - 1) );

        // Sample the reflection color at the specified screen position and roughness level
        return ReflectionColor.SampleLevel( g_sTrilinearClamp, ScreenPosition * g_vInvViewportSize, flLevel ); // Could probably use SampleScreenSsMSAA
    }

    static bool IsEnabled()
    {
        return ReflectionColorIndex != -1;
    }
};


#endif // DYNAMIC_REFLECTIONS_HLSL