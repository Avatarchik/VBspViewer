﻿Shader "Custom/WorldGeometry"
{
    Properties
    {
        _LightMap( "Light Map (RGB)", 2D ) = "white" {}
        _AmbientColor( "Ambient (RGB)", Color) = (0.25, 0.25, 0.25, 1)
    }

    SubShader
    {
        Tags{ "RenderType" = "Opaque" }
        LOD 200

        Pass
        {
            Tags{ "LightMode" = "ForwardBase" }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdadd_fullshadows
            #include "WorldGeometryShared.cginc"
            ENDCG
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }

            Cull Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #define WORLD_SHADOW_CASTER
            #include "WorldGeometryShared.cginc"
            ENDCG
        }
    }
}
