
using UnityEngine;
using System.Collections;

public static class NoiseQuery {

	public static float RequestNoisePoint(int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset) {

		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;
		
		for (int i = 0; i < octaves; i++)
		{
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) - offset.y;
			octaveOffsets[i] = new Vector2(offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}
		

		if (scale <= 0) {
			scale = 0.0001f;
		}

        float noiseHeight = 0;
		amplitude = 1;
		frequency = 1;


        for (int i = 0; i < octaves; i++) {
            float sampleX = (octaveOffsets[i].x + offset.x)/ scale * frequency;
            float sampleY = (octaveOffsets[i].y + offset.y)/ scale * frequency;

            float perlinValue = Mathf.PerlinNoise (sampleX, sampleY) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= persistance;
            frequency *= lacunarity;
        }


        float normalizedHeight = Mathf.Clamp((noiseHeight+ 1) / (maxPossibleHeight), 0, 1);

        return normalizedHeight;

	}

}