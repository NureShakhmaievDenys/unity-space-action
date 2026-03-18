Shader "Custom/VoxelShipURP"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
        [Header(Rim Lighting)]
        _RimColor ("Rim Color", Color) = (0.5, 0.7, 1, 1)
        _RimPower ("Rim Power", Range(0.1, 8.0)) = 3.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            // Подключаем библиотеки URP
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0; // Координаты для текстуры
                float4 color        : COLOR; 
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float2 uv           : TEXCOORD2; // Пробрасываем UV во фрагментный шейдер
                float4 color        : COLOR;
            };

            // Объявляем текстуру и сэмплер по правилам URP для лучшей производительности
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST; // Данные для Tiling и Offset
                half4 _BaseColor;
                half4 _RimColor;
                half _RimPower;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                
                // Применяем Tiling и Offset из настроек материала к UV
                OUT.uv = IN.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // Считываем цвет пикселя из текстуры
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // Получаем данные главного источника света
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color;

                // Базовый цвет (Текстура * Цвет материала * Вершинный цвет)
                half3 albedo = texColor.rgb * _BaseColor.rgb * IN.color.rgb;

                // 1. Диффузное освещение
                half NdotL = saturate(dot(normalWS, lightDir));
                half3 diffuse = lightColor * NdotL * albedo;

                // 2. Окружающий свет
                half3 ambient = SampleSH(normalWS) * albedo;

                // 3. ДИНАМИЧЕСКИЙ Контурный свет (Dynamic Rim Light)
                half rim = 1.0 - saturate(dot(viewDirWS, normalWS));
                rim = pow(rim, _RimPower);
                
                // Высчитываем влияние направления света на контур.
                // +0.5 позволяет свету немного "огибать" корабль, чтобы контур не обрывался слишком резко.
                half rimLightDirection = saturate(dot(normalWS, lightDir) + 0.5);
                
                // Теперь Rim Light умножается на направление и цвет главного источника света
                half3 rimLight = rim * rimLightDirection * lightColor * _RimColor.rgb * _RimColor.a;

                // Собираем всё вместе
                half3 finalColor = diffuse + ambient + rimLight;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}