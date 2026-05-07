Shader "UI/LowHealthHeartbeat"
{
    Properties
    {
        _Color ("Color", Color) = (0.8, 0, 0, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.35
        _VignetteStrength ("Vignette Strength", Range(0, 5)) = 2.2
        _PulseSpeed ("Pulse Speed", Range(0.1, 5)) = 1
        _HealthFade ("Health Fade", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
            "PreviewType"="Plane"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;
            float _Intensity;
            float _VignetteStrength;
            float _PulseSpeed;
            float _HealthFade;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float HeartbeatPulse(float t)
            {
                float cycle = frac(t * _PulseSpeed);

                float beat1 = exp(-pow((cycle - 0.08) * 22.0, 2.0));
                float beat2 = exp(-pow((cycle - 0.22) * 22.0, 2.0)) * 0.75;

                return saturate(beat1 + beat2);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV) * 2.0;

                float vignette = saturate(pow(dist, _VignetteStrength));

                float pulse = HeartbeatPulse(_Time.y);

                float alpha = vignette * _Intensity * _HealthFade;
                alpha *= 0.65 + pulse * 0.35;

                fixed4 col = _Color;
                col.a = alpha;

                return col;
            }
            ENDCG
        }
    }
}