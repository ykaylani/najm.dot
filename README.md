# astra.dot

A high-performance, real-time N-Body gravity simulation implemented in Unity's Data-Oriented Tech Stack (DOTS). It leverages the Barnes-Hut algorithm and Morton encoding to efficiently simulate thousands of gravitationally interacting bodies with physically accurate initial conditions.

## Features
- **High-Performance Barnes-Hut Algorithm**: Reduces complexity from O(n²) to O(n log n) using a morton-encoded octree built with a parallel radix sort.
- **Data-Oriented Design**: Struct-Of-Arrays used for Processing to achieve High-Performance while also converting the SoA into AoS for easy editing.
- **Physically Accurate Initial Conditions**: Generates stable Keplerian orbits from standard orbital elements (semimajor axis, eccentricity, inclination, etc.).
- **Massively Parallel CPU Computation**: Fully multithreaded using Unity's Job System and Burst compiler for optimal performance.
- **Custom Tooling**: An intuitive custom inspector for managing simulation parameters and body properties.

## Getting Started

### Firstly, clone this repository: </h4>

```
git clone https://github.com/sandalconsumer/unity-n-body.git
```
### Secondly, to open it in Unity:</h4>

1. Open Unity Hub.
2. Click "Add Project" and select the cloned repository folder to open it.
- The version this project was developed in is Unity 6000.1.1f1 and that is the version where the best experience lies, but all Unity 6 versions will most likely work with this.

### Finally, to open the example Scene in your Unity editor: </h4>

- Navigate to **Assets/Scenes** then Click on the **SampleScene** to open it!

### Running The Simulation</h4>

- Press the play button in the Unity editor  

## Technical Details

- The Integrator used in this simulation is Symplectic (Semi-Implicit) Euler.
- The Barnes-Hut Algorithm plays a role in making the simulation more performant by making the physics calculations O(n log n) instead of O(n²).
- Double Precision was used instead of float because of Small Precision errors accumulating over time, causing a big error. Double Precision also has a bigger range for massive force (10<sup>-324</sup> to 10<sup>308</sup>) while float has 10<sup>-38</sup> to 10<sup>38</sup>

## Roadmap

### Major:  
 - Delinking Propagation from Unity's FixedUpdate (Predictive Simulation)
 - Custom Mesh Generation for Orbit Trails

### Minor:

- Floating Origin for Flexibility  
- Non-Singleton Propagator for Multiple Simulations / Scene  
- Major Event Triggers (Body Collisions, Orbit Escape, etc.)  
- Multiple Integrators (Velocity Verlet, Symplectic Euler, Leapfrog + Yoshida)  

## Credits and Resources

This project was made possible with the help of these amazing resources:  
[Barnes-Hut Algorithm Documentation on arborjs.org](https://arborjs.org/docs/barnes-hut)  
[Arcane Algorithm Archive's Verlet Integration Documentation](https://www.algorithm-archive.org/contents/verlet_integration/verlet_integration.html)  
[Semi-Implicit Euler - Wikipedia](https://en.wikipedia.org/wiki/Semi-implicit_Euler_method)  
[Orbital Mechanics - Wikipedia](https://en.wikipedia.org/wiki/Orbital_mechanics)  
[Perifocal coordinate system - Wikipedia](https://en.wikipedia.org/wiki/Perifocal_coordinate_system)  
[John Sietsma - Morton Order](https://johnsietsma.com/2019/12/05/morton-order-introduction/)  
[W3 Schools - DSA Radix Sort](https://www.w3schools.com/dsa/dsa_algo_radixsort.php)  
