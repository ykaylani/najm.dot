using Unity.Mathematics;
using UnityEngine;

public enum TIntegrator
{
    SIEuler,
    VelocityVerlet
}

public static class Integrator
{
    public static void SIEuler(ref double3 position, ref double3 velocity, ref double3 force, ref double mass)
    {
        velocity += Time.fixedDeltaTime * (force / mass);
        position += Time.fixedDeltaTime * velocity;
        force = double3.zero;
    }

    public static void VVerlet1(ref double3 position, ref double3 velocity, ref double3 force, ref double mass)
    {
        velocity += 0.5 * (force / mass) * Time.fixedDeltaTime;
        position += velocity * Time.fixedDeltaTime;
        force = double3.zero;
    }

    public static void VVerlet2(ref double3 velocity, ref double3 force, ref double mass)
    {
        velocity += 0.5 * (force / mass) * Time.fixedDeltaTime;
    }
}
