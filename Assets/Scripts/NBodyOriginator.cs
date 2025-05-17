using UnityEngine;
using System.Collections.Generic;

public class NBodyOriginator : MonoBehaviour
{
    public float gravitationalConstant = 6.67e-11f;
    public float distMultiplier = 5e9f; // 5 billion times bigger
    public float epsilon = 0.05f; // 1 million times faster
    public List<NBody> bodies = new List<NBody>();
    
}
