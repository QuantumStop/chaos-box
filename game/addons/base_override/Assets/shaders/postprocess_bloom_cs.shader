HEADER
{
    DevShader = true;
    Description = "Compute shader for Bloom accumulation";
}

MODES
{
    Default();
}

COMMON
{
    #include "postprocess/shared.hlsl"

    // 0 = Bilinear (fast), 1 = Biquadratic (quality)
    DynamicCombo( D_FILTER, 0..1, Sys( ALL ) );
}

CS
{
    // Samplers
    SamplerState BilinearBorder      < Filter(BILINEAR); AddressU(BORDER); AddressV(BORDER); AddressW(BORDER); >;

    // Inputs
    Texture2D Color                 < Attribute("Color"); >;
    Texture2D QuarterResEffectsBloomInputTexture < Attribute( "QuarterResEffectsBloomInputTexture" ); >;

    // Output
    RWTexture2D<float4> BloomOut    < Attribute("BloomOut"); >;

    // Params
    float Strength                  < Attribute("Strength"); Default(0.0f); >;
    float Threshold                 < Attribute("Threshold"); Default(0.0f); >;
    float Gamma                     < Attribute("Gamma"); Default(2.2f); >;
    float3 Tint                     < Attribute("Tint"); Default3(1.0f, 1.0f, 1.0f); >;
    float2 InvDimensions            < Attribute("InvDimensions"); >;

    float3 SampleBiquadraticLevel(Texture2D tex, float2 uv, int level)
    {
        // Approximate biquadratic by averaging four bilinear taps
        int2 texSize;
        tex.GetDimensions(texSize.x, texSize.y);
        texSize >>= level;
        float2 invTexSize = 1.0 / float2(texSize);

        float2 texelPos = uv * float2(texSize);
        float2 fracOffset = frac(texelPos);
        float2 baseTexel = floor(texelPos);

        const float2 offset1 = float2(-1.0, -1.0) / 3.0;
        const float2 offset2 = float2( 1.0, -1.0) / 3.0;
        const float2 offset3 = float2(-1.0,  1.0) / 3.0;
        const float2 offset4 = float2( 1.0,  1.0) / 3.0;

        float2 samplePos1 = (baseTexel + 0.5 + offset1 + fracOffset) * invTexSize;
        float2 samplePos2 = (baseTexel + 0.5 + offset2 + fracOffset) * invTexSize;
        float2 samplePos3 = (baseTexel + 0.5 + offset3 + fracOffset) * invTexSize;
        float2 samplePos4 = (baseTexel + 0.5 + offset4 + fracOffset) * invTexSize;

        float3 color = 0;
        color += tex.SampleLevel(BilinearBorder, samplePos1, level).rgb * 0.25;
        color += tex.SampleLevel(BilinearBorder, samplePos2, level).rgb * 0.25;
        color += tex.SampleLevel(BilinearBorder, samplePos3, level).rgb * 0.25;
        color += tex.SampleLevel(BilinearBorder, samplePos4, level).rgb * 0.25;
        return color;
    }

    float3 SampleBilinearLevel(Texture2D tex, float2 uv, int level)
    {
        // Apply half-pixel offset cuz of how we do fast gaussian
        int2 texSize;
        tex.GetDimensions(texSize.x, texSize.y);
        texSize >>= level;
        float2 invTexSize = 1.0 / float2(texSize);

        float2 texelPos = uv * float2(texSize);
        float2 baseTexel = floor(texelPos);
        float2 fracOffset = frac(texelPos);

        float2 samplePos = (baseTexel + 0.5 + fracOffset) * invTexSize;
        return tex.SampleLevel(BilinearBorder, samplePos, level).rgb;
    }

    float3 SampleLevelMode(Texture2D tex, float2 uv, int level)
    {
        #if D_FILTER == 1
            return SampleBiquadraticLevel(tex, uv, level);
        #else
            return SampleBilinearLevel(tex, uv, level);
        #endif
    }

    float3 SampleBloom(Texture2D tex, float2 uv, float gammaCorrection, float falloffScale)
    {
        uint width, height, mipLevels;
        tex.GetDimensions(0, width, height, mipLevels);

        float3 bloom = 0;

        [unroll]
        for (int i = 0; i < (int)mipLevels - 1; i++)
        {
            float3 sample = SampleLevelMode(tex, uv, i + 1);
            bloom += max(pow(sample, gammaCorrection), 0) * exp2(-falloffScale * i);
        }

        return bloom;
    }

    [numthreads(8,8,1)]
    void MainCs(uint3 DTid : SV_DispatchThreadID)
    {
        // Get bloom render target dimensions
        uint bloomWidth, bloomHeight;
        BloomOut.GetDimensions(bloomWidth, bloomHeight);
        
        // Calculate UV coordinates properly for half-resolution target
        float2 uv = (DTid.xy ) / float2(bloomWidth, bloomHeight);

        // Bloom parameters
        const float bloomIntensity = pow(Strength * 0.1, 2);
        const float falloffScale = ( Threshold - 2 );
        const float gammaCorrection = Gamma;

        float3 bloomColor = 0;
        bloomColor += SampleBloom(Color, uv, gammaCorrection, falloffScale) * bloomIntensity;

        // Add quarter-res bloom-only effects
        float3 qsample = SampleBloom( QuarterResEffectsBloomInputTexture, uv, gammaCorrection, falloffScale);
        bloomColor += qsample;

        bloomColor *= Tint;

        BloomOut[DTid.xy] = float4(bloomColor, 1.0);
    }
}

