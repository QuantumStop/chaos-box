

//
// Blend Modes (https://web.dev/learn/css/blend-modes/)
// I only filled in what I needed. A job for someone else - garry
//
DynamicCombo( D_BLENDMODE, 0..2, Sys( ALL ) );

// Alpha Blend
#if D_BLENDMODE == 0
    RenderState( BlendEnable, true );
    RenderState( SrcBlend, SRC_ALPHA );
    RenderState( DstBlend, INV_SRC_ALPHA );
    RenderState( BlendOp, ADD );
    RenderState( SrcBlendAlpha, ONE );
    RenderState( DstBlendAlpha, INV_SRC_ALPHA );
    RenderState( BlendOpAlpha, ADD );
// Multiply
#elif D_BLENDMODE == 1
    RenderState( BlendEnable, true );
    RenderState( SrcBlend, DEST_COLOR );
    RenderState( DstBlend, INV_SRC_ALPHA );
    RenderState( SrcBlendAlpha, ONE );
    RenderState( DstBlendAlpha, ONE );
// Lighten
#elif D_BLENDMODE == 2
    RenderState( BlendEnable, true );
    RenderState( SrcBlend, SRC_ALPHA );
    RenderState( DstBlend, ONE );
    RenderState( SrcBlendAlpha, ONE );
    RenderState( DstBlendAlpha, ONE );
#endif
