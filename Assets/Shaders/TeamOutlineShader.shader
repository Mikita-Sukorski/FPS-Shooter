Shader "Custom/TeamOutline"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (1,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.2)) = 0.05
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        
        Pass
        {
            Name "OUTLINE"
            Cull Front // Отрисовываем только задние (внутренние) полигоны
            ZWrite On
            
            // Расскомментируй строку ниже, если хочешь чтобы обводку было видно СКВОЗЬ СТЕНЫ:
            // ZTest Always 

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            v2f vert(appdata v) {
                v2f o;
                // Сдвигаем вершины по их нормалям (увеличиваем модель)
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target {
                return _OutlineColor; // Возвращаем чистый цвет обводки
            }
            ENDCG
        }
    }
}