﻿Shader "Custom/WorldGeometry.NoCull.Translucent"
{
    Properties
    {
        _MainTex( "Albedo (RGB)", 2D ) = "white" {}
        _LightMap( "Light Map (RGB)", 2D ) = "white" {}
        _AmbientColor( "Ambient (RGB)", Color) = (0.25, 0.25, 0.25, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
        }

        LOD 200

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define TRANSLUCENT
            #include "WorldGeometryShared.cginc"
            ENDCG
        }
    }
}
