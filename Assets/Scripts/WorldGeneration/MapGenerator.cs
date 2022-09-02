using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { noiseMap, Mesh };
    public DrawMode drawMode;

    public NoiseData noiseData;
    public MeshSettings meshSettings;

    public bool autoUpdate;

    static MapGenerator instance;

    static Dictionary<string, Queue<threadData>> indexedThreadD = new Dictionary<string, Queue<threadData>>();

    Queue<threadInfo> dataInfQ = new Queue<threadInfo>();

    void Update() {
        if (dataInfQ.Count > 0) {
            for (int i = 0; i < dataInfQ.Count; i++) {
                threadInfo threadInfo = dataInfQ.Dequeue();
                threadInfo.callback(threadInfo.parameter);
            }

        }
    }

    void Awake() {
        instance = FindObjectOfType<MapGenerator>();
    }

    public static void RequestData(Func<object> generateDatum, Action<object> callback) {
        ThreadStart threadStart = delegate {
            instance.dataThread(generateDatum, callback);
        };

        new Thread(threadStart).Start();
    }

    void dataThread(Func<object> generateDatum, Action<object> callback) {
        object data = generateDatum();
        lock (dataInfQ)
        {
            dataInfQ.Enqueue(new threadInfo(callback, data));
        }
    }

    public static void RequestIndexedData(Func<object> generateDatum, Action<object> callback, string index)
    {
        if (!indexedThreadD.ContainsKey(index))
            NewIndexedData(generateDatum, callback, index);

        lock (indexedThreadD[index])
        {
            if (indexedThreadD[index].Count == 0)
                NewIndexedData(generateDatum, callback, index);
            else
                indexedThreadD[index].Enqueue(new threadData(generateDatum, callback));
        }
    }

    public static void NewIndexedData(Func<object> generateDatum, Action<object> callback, string index)
    {
        threadData baseData = new threadData(generateDatum, callback);
        Queue<threadData> baseQueue = new Queue<threadData>();
        baseQueue.Enqueue(baseData);

        indexedThreadD[index] = baseQueue;

        ThreadStart threadStart = delegate {
            instance.IndexedDataThread(baseQueue);
        };

        new Thread(threadStart).Start();
    }

    void IndexedDataThread(Queue<threadData> generateData)
    {
        threadData thread;
        while (true)
        {
            lock (generateData)
            {
                if (generateData.Count != 0)
                    thread = generateData.Peek();
                else
                    break;
            }

            object data = thread.threadFunc();
            lock (dataInfQ)
            {
                dataInfQ.Enqueue(new threadInfo(thread.callback, data));
            }

            lock (generateData)
            {
                generateData.Dequeue();
            }
        }
    }

    struct threadData
    {
        public Func<object> threadFunc;
        public Action<object> callback;

        public threadData(Func<object> threadFunc, Action<object> callback)
        {
            this.threadFunc = threadFunc;
            this.callback = callback;
        }
    }

    struct threadInfo{
        public readonly Action<object> callback;
        public readonly object parameter;

        public threadInfo(Action<object> callback, object parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }
    }
}
