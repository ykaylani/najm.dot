using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(Scraper))]
public class ProcessTime : MonoBehaviour
{
    private Scraper scraper;
    private Stopwatch stopwatch = new Stopwatch();
    
    private NativeArray<double> propagatorMS;

    void Start()
    {
        scraper = GetComponent<Scraper>();
        propagatorMS = new NativeArray<double>(scraper.maxRecord, Allocator.Persistent);
    }

    public void PropagatorCallStart()
    {
        stopwatch.Start();
    }

    public void PropagatorCallEnd()
    {
        stopwatch.Stop();
    }

    public void Read(int currentStep)
    {
        TimeSpan time = stopwatch.Elapsed;
        propagatorMS[currentStep] = time.TotalMilliseconds;
        stopwatch.Reset();
    }

    void Export()
    {
        if (!this.enabled) return;
        
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Propagator Time");

        for (int i = 0; i < propagatorMS.Length; i++) { if(propagatorMS[i] != 0){ stringBuilder.AppendLine(propagatorMS[i].ToString("G17")); } }
        string filePath = Path.Combine(Application.persistentDataPath, "PropagatorMS_" + DateTime.Now.ToFileTime() + ".csv");
        File.WriteAllText(filePath, stringBuilder.ToString());
        UnityEngine.Debug.Log($"Propagator Time Saved To {filePath}");
    }

    void OnDestroy()
    {
        Export();
        
        propagatorMS.Dispose();
    }
    
}
