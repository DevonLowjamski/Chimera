Shader "ProjectChimera/BlueprintOverlay"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.6, 1.0, 0.4)
        _OutlineColor ("Outline Color", Color) = (1.0, 1.0, 1.0, 0.8)
        _OutlineWidth ("Outline Width", Range(0.001, 0.1)) = 0.02
        _PulseIntensity ("Pulse Intensity", Range(0.0, 1.0)) = 0.3
        _PulseSpeed ("Pulse Speed", Range(0.0, 10.0)) = 2.0
        _FadeValue ("Fade Value", Range(0.0, 1.0)) = 1.0
        _GridSize ("Grid Size", Float) = 1.0
        _GridOpacity ("Grid Opacity", Range(0.0, 1.0)) = 0.5
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Pass
        {
            Name "BlueprintMain"
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                UNITY_FOG_COORDS(3)
            };
            
            float4 _BaseColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _PulseIntensity;
            float _PulseSpeed;
            float _FadeValue;
            float _GridSize;
            float _GridOpacity;
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o, o.pos);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                // Base color with fade
                float4 color = _BaseColor;
                color.a *= _FadeValue;
                
                // Pulse animation
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                color.a *= pulse;
                
                // Grid pattern
                float2 gridUV = i.uv * _GridSize;
                float2 grid = abs(frac(gridUV - 0.5) - 0.5);
                float gridLine = min(grid.x, grid.y);
                float gridMask = step(gridLine, 0.05);
                
                // Combine grid with base color
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, gridMask * _GridOpacity);
                
                // Edge detection for outline effect
                float3 viewDir = normalize(_WorldSpaceCameraPos - i.worldPos);
                float fresnel = 1.0 - saturate(dot(i.worldNormal, viewDir));
                fresnel = pow(fresnel, 2.0);
                
                // Apply outline
                color.rgb = lerp(color.rgb, _OutlineColor.rgb, fresnel * _OutlineColor.a);
                
                // Apply fog
                UNITY_APPLY_FOG(i.fogCoord, color);
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "Legacy Shaders/Transparent/Diffuse"
}