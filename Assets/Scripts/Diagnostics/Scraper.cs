using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Propagator))]
public class Scraper : MonoBehaviour
{
    private Propagator propagator;
    private EnergyTracker energyTracker;
    private ProcessTime processTime;
    
    public int timestepPollingRate = 150;
    public int maxRecord = 200;
    
    private int currentStep = 0;
    private int pollAggregateTime;
    
    private NativeArray<double3> positions => propagator.bodies.positions;
    private NativeArray<double3> velocities  => propagator.bodies.velocities;
    private NativeArray<double> masses  => propagator.bodies.masses;

    void OnEnable()
    {
        Propagator.pingS += PropagatorPingSCall;
        Propagator.pingE += PropagatorPingECall;
    }

    void Start()
    {
        propagator = GetComponent<Propagator>();
        TryGetComponent<EnergyTracker>(out energyTracker);
        TryGetComponent<ProcessTime>(out processTime);
    }

    void FixedUpdate()
    {
        pollAggregateTime += 1;

        if (pollAggregateTime >= timestepPollingRate)
        {
            ComputeCall();
            pollAggregateTime = 0;
            currentStep += 1;
        }
    }
    
    
    void ComputeCall()
    {
        if (currentStep >= maxRecord - 1) {Debug.LogWarning("Max Diagnostics Record Reached, Increase 'Max Record' on Scraper."); EditorApplication.isPlaying = false;}
        if (energyTracker && energyTracker.enabled) energyTracker.Compute(currentStep, positions, velocities, masses);
    }
    
    void PropagatorPingSCall()
    {
        if(processTime) processTime.PropagatorCallStart();
    }

    void PropagatorPingECall()
    {
        if (currentStep >= maxRecord - 1) {Debug.LogWarning("Max Diagnostics Record Reached, Increase 'Max Record' on Scraper."); EditorApplication.isPlaying = false;}
        
        if (processTime && processTime.enabled) processTime.PropagatorCallEnd();
        if (processTime && processTime.enabled) processTime.Read(currentStep);
    }
}
