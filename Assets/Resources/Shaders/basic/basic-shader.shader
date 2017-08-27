Shader "Basic Shader" {
    
    Properties {
        _Color("Main Color", Color) = (1,1,0,1)
    }
    
    SubShader {

        Tags { "LightMode" = "ForwardBase" }

        Pass {
            CGPROGRAM
            #include "UnityCG.cginc"
            #pragma target 5.0  
            #pragma vertex vert
            #pragma fragment frag

            struct appdata {
                float4 vertex : POSITION;
                float4 normal : NORMAL;
            }; 

            struct v2f {
                float4 pos : SV_POSITION;
                float4 col : COLOR;
            };

            uniform fixed4 _LightColor0;
            float4 _Color;

            v2f vert(appdata i)
            {
                v2f o;

                // Position
                float4 pos = i.vertex;
                float4 nor = i.normal;
                o.pos = UnityObjectToClipPos(pos);

                // Lighting
                float3 normalDirection = normalize(nor.xyz);
                float4 AmbientLight = UNITY_LIGHTMODEL_AMBIENT;
                float4 LightDirection = normalize(_WorldSpaceLightPos0);
                float4 DiffuseLight = saturate(dot(LightDirection, normalDirection))*_LightColor0;
                o.col = float4(AmbientLight + DiffuseLight);
                
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 final = _Color;
                final *= i.col;
                return final;
            }

            ENDCG
        }
    }
}