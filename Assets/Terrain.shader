
Shader "Custom/Terrain"
{
    Properties{

    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows


        #pragma target 3.0

        const static int maxLayerCount = 8;
        const static float epsilon = 1E-4;


        int layerCount;
        float3 baseColors[maxLayerCount];
        float baseStartHeights[maxLayerCount];
        float noiseStartHeights[maxLayerCount];
        float baseBlends[maxLayerCount];
        float baseColorStrength[maxLayerCount];
        float baseTextureScales[maxLayerCount];
        float baseTextureIndexes[maxLayerCount];

        float minHeight;
        float maxHeight;

        int neighborLayerCount;
        float neighborIndToLayer[8*maxLayerCount];
        float3 neighborBaseColors[8*maxLayerCount];
        float neighborBaseStartHeights[8*maxLayerCount];
        float neighborBaseBlends[8*maxLayerCount];
        float neighborBaseColorStrength[8*maxLayerCount];
        float neighborBaseTextureScales[8*maxLayerCount];
        float neighborBaseTextureIndexes[8*maxLayerCount];

        float boundUp;
        float boundRight;
        float boundDown;
        float boundLeft;

        int blendDistance;

        float TTVertexC;
        sampler2D TTNoise;

        UNITY_DECLARE_TEX2DARRAY(baseTextures);

        struct Input
        {
            float3 worldPos;
            float3 worldNormal;
        };

        float inverseLerp(float a, float b, float value){
            return saturate((value - a)/(b-a));
        }

        float3 triplaner(float3 worldPos, float scale, float3 blendAxes, int textureIndex){
            float3 scaledWorldPos = worldPos/scale;

            float3 xProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.y, scaledWorldPos.z, textureIndex)) * blendAxes.x;
            float3 yProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.z, textureIndex)) * blendAxes.y;
            float3 zProjection = UNITY_SAMPLE_TEX2DARRAY(baseTextures, float3(scaledWorldPos.x, scaledWorldPos.y, textureIndex)) * blendAxes.z;
            return xProjection + yProjection + zProjection;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float heightPercent = inverseLerp(minHeight, maxHeight, IN.worldPos.y);
            float3 blendAxes = abs(IN.worldNormal);
            blendAxes /= blendAxes.x + blendAxes.y + blendAxes.z;

            float noiseSample = tex2Dlod(TTNoise, float4((IN.worldPos.xy - float2(boundLeft, boundDown))/float2(boundRight-boundLeft, boundUp-boundDown), 0, 1));
            //float noiseSample = tex2Dlod(TTNoise, float4(0, (IN.worldPos.y - boundDown)/coordsPerVertTT, 0, 1));
            //float noiseSample = UNITY_SAMPLE_TEX2DARRAY(TTNoise, float3((IN.worldPos.xy - float2(boundLeft, boundDown))/coordsPerVertTT, 0))
            for(int i = 0; i < layerCount; i++){
                float drawStrength = inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, heightPercent - baseStartHeights[i]) * 0.9 
                                    + 0.1 * inverseLerp(-baseBlends[i]/2 - epsilon, baseBlends[i]/2, noiseSample - noiseStartHeights[i]);
                float3 baseColor = baseColors[i] * baseColorStrength[i];
                float3 textureColor = triplaner(IN.worldPos, baseTextureScales[i], blendAxes, baseTextureIndexes[i]) * (1 - baseColorStrength[i]);
                o.Albedo = o.Albedo * (1-drawStrength) + (baseColor + textureColor) * drawStrength;//noiseSample;
            }

            const float xBounds[8] = {boundLeft, IN.worldPos.x, boundRight, boundLeft, boundRight, boundLeft, IN.worldPos.x, boundRight};
            const float yBounds[8] = {boundDown, boundDown, boundDown, IN.worldPos.z, IN.worldPos.z, boundUp, boundUp, boundUp};
            float3 neighborAlbedo = 1;

            //same code to calculate color
            for(int curI = 0; curI < neighborLayerCount; curI++){
                float drawStrengthHorizontal = (blendDistance - (abs(IN.worldPos.x - xBounds[neighborIndToLayer[curI]]) + abs(IN.worldPos.z - yBounds[neighborIndToLayer[curI]]))) / (2*blendDistance);
                if(drawStrengthHorizontal >= 0){
                    float drawStrengthVertical = inverseLerp(-neighborBaseBlends[curI]/2 - epsilon, neighborBaseBlends[curI]/2, heightPercent - neighborBaseStartHeights[curI]);
                    float3 neighborBaseColor = neighborBaseColors[curI] * neighborBaseColorStrength[curI];
                    float3 neighborTextureColor = triplaner(IN.worldPos, neighborBaseTextureScales[curI], blendAxes, neighborBaseTextureIndexes[curI]) * (1 - neighborBaseColorStrength[curI]);
                    neighborAlbedo = neighborAlbedo * (1-drawStrengthVertical) + (neighborBaseColor + neighborTextureColor) * (drawStrengthVertical);
                }
                if(curI != neighborLayerCount - 1 && neighborIndToLayer[curI] != neighborIndToLayer[curI+1]){
                    if(drawStrengthHorizontal >= 0){
                        o.Albedo = o.Albedo * (1-drawStrengthHorizontal) + neighborAlbedo*drawStrengthHorizontal;   
                        float3 neighborAlbedo = 1;
                    }
                }
            }
            
        }
        ENDCG
    }
    FallBack "Diffuse"
}
