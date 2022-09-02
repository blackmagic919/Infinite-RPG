using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PureNoiseGen
{
    public static Texture2D Create(int resolution, int seed)
    {
        Texture2D noise = new Texture2D(resolution, resolution);
        System.Random rand = new System.Random(seed);
        for (int x = 0; x < resolution; x++)
        {
            for (int y = 0; y < resolution; y++)
            {
                noise.SetPixel(x, y, Color.white * ((float)rand.Next(resolution)/ resolution));
            }
        }
        noise.Apply();
        return noise;
    }

    public static Texture2D ConvertToTexture(float[,] array)
    {
        float[] one = new float[array.GetLength(0)];
        float[] two = new float[array.GetLength(1)];
        Texture2D noise = new Texture2D(array.GetLength(0), array.GetLength(1));
        for (int x = 0; x < array.GetLength(0); x++)
        {
            for (int y = 0; y < array.GetLength(1); y++)
            {
                noise.SetPixel(x, y, Color.white * array[x,y]);
                if (x == 3) one[y] = array[3, y];
            }
            two[x] = array[x, 5];
        }
        noise.Apply();
        return noise;
    }
}
