
using UnityEngine;
using System.Collections;

public static class Noise {

	public enum NormalizeMode {Local, Global};

	// zero zero is bottom left
	public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, int skipInc, Vector2 offset) {
		float[,] noiseMap = new float[mapWidth/skipInc + 1, mapHeight/skipInc + 1];

		System.Random prng = new System.Random (seed);
		Vector2[] octaveOffsets = new Vector2[octaves];

		float maxPossibleHeight = 0;
		float amplitude = 1;
		float frequency = 1;

		for (int i = 0; i < octaves; i++) {
			float offsetX = prng.Next(-100000, 100000) + offset.x;
			float offsetY = prng.Next(-100000, 100000) - offset.y;
			octaveOffsets [i] = new Vector2 (offsetX, offsetY);

			maxPossibleHeight += amplitude;
			amplitude *= persistance;
		}

		if (scale <= 0) {
			scale = 0.0001f;
		}
		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;

		for (int y = 0; y < mapHeight; y += skipInc) {
			for (int x = 0; x < mapWidth; x += skipInc) {

				amplitude = 1;
				frequency = 1;
				float noiseHeight = 0;
				for (int i = 0; i < octaves; i++) {
					float sampleX = ((x-halfWidth + octaveOffsets[i].x) / scale) * frequency;
					float sampleY = ((y-halfHeight + octaveOffsets[i].y) / scale) * frequency;

					float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
					noiseHeight += perlinValue * amplitude;

					amplitude *= persistance;
					frequency *= lacunarity;
				}

				noiseMap [x/skipInc, y/skipInc] = noiseHeight;
			}
		}

		for (int y = 0; y < mapHeight/skipInc; y++) {
			for (int x = 0; x < mapWidth/skipInc; x++) {
					float normalizedHeight = (noiseMap [x, y] + 1) / (maxPossibleHeight/0.9f);
					noiseMap [x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
			}
		}

		return noiseMap;
	}

	public static float[,][] GenerateSurroundNoise(int mapWidth, int mapHeight, NoiseInputs[] surroundNoise, float blendDist, int skipInc, Vector2 offset)
	{
		blendDist += skipInc;
		Vector2[] start = new Vector2[8] {new Vector2(0, 0), new Vector2(mapWidth - blendDist, 0), new Vector2(0, mapHeight - blendDist), new Vector2(0, 0), 
										new Vector2(mapWidth-blendDist, 0), new Vector2(mapWidth - blendDist, mapHeight - blendDist), new Vector2(0, mapHeight-blendDist), new Vector2(0, 0)};
		Vector2[] end = new Vector2[8] {new Vector2(mapWidth, blendDist), new Vector2(mapWidth, mapHeight), new Vector2(mapWidth, mapHeight), new Vector2(blendDist, mapHeight), 
										new Vector2(mapWidth, blendDist), new Vector2(mapWidth, mapHeight), new Vector2(blendDist, mapHeight), new Vector2(blendDist, blendDist)};

		float[,][] noiseMap = new float[mapWidth / skipInc + 1, mapHeight / skipInc + 1][];

		Vector2[][] octaveOffsets = new Vector2[surroundNoise.Length][];
		float[] maxPossibleHeight = new float[surroundNoise.Length];

		float amplitude = 1;
		float frequency = 1;

		for (int o = 0; o < 8; o++)
        {
			System.Random prng = new System.Random(surroundNoise[o].localSeed);
			octaveOffsets[o] = new Vector2[surroundNoise[o].octaves];
			amplitude = 1;

			for (int i = 0; i < surroundNoise[o].octaves; i++)
			{
				float offsetX = prng.Next(-100000, 100000) + offset.x;
				float offsetY = prng.Next(-100000, 100000) - offset.y;
				octaveOffsets[o][i] = new Vector2(offsetX, offsetY);

				maxPossibleHeight[o] += amplitude;
				amplitude *= surroundNoise[o].persistance;
			}

			if (surroundNoise[o].noiseScale <= 0)
			{
				surroundNoise[o].noiseScale = 0.0001f;
			}
		}

		float halfWidth = mapWidth / 2f;
		float halfHeight = mapHeight / 2f;


		for (int o = 0; o < 8; o++)
		{

			for(int y = (int)start[o].y; y < (int)end[o].y; y += skipInc)
            {
				for (int x = (int)start[o].x; x < (int)end[o].x; x += skipInc)
				{
					if (noiseMap[x / skipInc, y / skipInc] == null)
						noiseMap[x / skipInc, y / skipInc] = new float[8] {-1, -1, -1, -1, -1, -1, -1, -1};

					amplitude = 1;
					frequency = 1;
					float noiseHeight = 0;

					for (int i = 0; i < surroundNoise[o].octaves; i++)
					{
						float sampleX = (x - halfWidth + octaveOffsets[o][i].x) / surroundNoise[o].noiseScale * frequency;
						float sampleY = (y - halfHeight + octaveOffsets[o][i].y) / surroundNoise[o].noiseScale * frequency;

						float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
						noiseHeight += perlinValue * amplitude;

						amplitude *= surroundNoise[o].persistance;
						frequency *= surroundNoise[o].lacunarity;
					}

					noiseMap[x / skipInc, y / skipInc][o] = noiseHeight;
				}
			}

			for (int y = (int)start[o].y; y < (int)end[o].y; y += skipInc)
			{
				for (int x = (int)start[o].x; x < (int)end[o].x; x += skipInc)
				{
					float normalizedHeight = (noiseMap[x / skipInc, y / skipInc][o] + 1) / (maxPossibleHeight[o] / 0.9f);
					noiseMap[x / skipInc, y / skipInc][o] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
				}
			}
		}


		return noiseMap;
	}

}
