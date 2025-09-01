using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Propagator))]
public class Scraper : MonoBehaviour
{
    private Propagator propagator;
    private EnergyTracker energyTracker;
    public int timestepPollingRate = 150;
    public int maxRecord = 200;
    
    private int currentStep = 0;
    private int pollAggregateTime = 0;
    
    private NativeArray<double3> positions;
    private NativeArray<double3> velocities;
    private NativeArray<double> masses;

    void Start()
    {
        propagator = GetComponent<Propagator>();
        TryGetComponent<EnergyTracker>(out energyTracker);
        
        positions = new NativeArray<double3>(propagator.bodies.positions.Length, Allocator.Persistent);
        velocities = new NativeArray<double3>(propagator.bodies.velocities.Length, Allocator.Persistent);
        masses = new NativeArray<double>(propagator.bodies.masses.Length, Allocator.Persistent);
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
        
        positions = propagator.bodies.positions;
        velocities = propagator.bodies.velocities;
        masses = propagator.bodies.masses;
        if (energyTracker) energyTracker.Compute(currentStep, positions, velocities, masses);
    }
}
