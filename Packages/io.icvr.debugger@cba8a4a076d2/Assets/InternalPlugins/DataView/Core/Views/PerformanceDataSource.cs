using UnityEngine;
using System.Collections.Generic;

public class PerformanceDataSource : MonoBehaviour
{
    public int maxSamples = 100;

    private readonly List<float> samples = new List<float>();

    public IReadOnlyList<float> Samples => samples;

    private void Update()
    {
        float ms = Time.deltaTime * 1000f;
        samples.Add(ms);

        if (samples.Count > maxSamples)
            samples.RemoveAt(0);
    }
}