# unity-n-body
N-Body Simulation implemented in Unity currently in development.  

## Features
- Gravitational N-Body Simulation  
- Double Precision Mathematics  
- Barnes-Hut Algorithm with Adaptive (and static) Simulation Bounds    
- Custom Unity Inspector UI  
- Keplerian Orbits  
- Orbit Visualization  
## Setup

<h4>Firstly, clone this repository: </h4>

```
git clone https://github.com/ykaylani/najm.dot.git
```

<h4>Secondly, to open it in Unity:</h4>

1. Open Unity Hub.
2. Click "Add Project" and select the cloned repository folder to open it.
- The version this project was developed in is Unity 6000.1.1f1 and that is the version where the best experience will be given, but all Unity 6 versions will most likely work with this.

<h4>Finally, to open the example Scene in your Unity editor: </h4>

- Navigate to **Assets/Scenes** then Click on the **SampleScene** to open it!

## Instructions

<h4>Running The Simulation</h4>
- Press the **Play** button in the Unity editor <br>
- the simulation should start automatically.

<h4>Interacting with the simulation in the Inspector</h4>

There are two Parts to this N-Body Simulation:

1. Propagator  
2. Body  

- The **Propagator** controls all the N-Bodies' movement and physics, and the **Bodies** are for storing the individual data of each body.

**Propagator Properties**

General
- Simulation Settings is 4 values: the first one (x) being the Distance Multiplier to convert from unity meters to simulation meters. The default is 1e9 or 1 billion, so 40 unity meters would be 40 billion simulation meters.  
- The second value (y) is the simulation timestep, which is how long it takes for the simulation to execute one timestep. The default is 0.02, so the simulation will run once every 0.02 seconds.  
- The third value (z) is the simulation bounds for Barnes-Hut.  
- the fourth value (w) is the padding for the bounds.  

Barnes-Hut
- the Opening Angle Criterion is the threshold that the Barnes-Hut algorithm will use to determine if it should use an approximation of mass or take the exact masses. values near 0 cause more accuracy but less prformance, and the opposite is true for values near 1. <br>

Bounds
- Adaptive Simulation bounds determines if the Simulation bounds will move to accomodate the bodies for accurate calculations with barnes-hut. As of Versions 0.4.x-alpha.1, it is better to disable IF you are using basic initial velocities as they have to chance to fling bodies thousands of meters away, and that causes the bounds to try and follow them.

**N-Body Properties**

General
- Mass is the mass of the object (in kg).

Initial Velocity Settings
- Initial Velocity is the velocity of the object at the beginning of the simulation. This property is only visible If **Keplerian Orbits** is turned off. It is generally not recommended to use this, as it can cause unstable orbits and object flinging.  
- Keplerian Orbits toggles whether of not orbits should be defined by orbital mechanics.  
- Eccentricity defines the shape of the orbit (0 = circle, 0<x<1 = ellipse, 1 = parabolic (escape velocity), x > 1 = hyperbolic (also escape velocity)).  
- Semimajor Axis defines the size of the orbit.  
- True Anomaly is the current position of the body along it's path from the periapsis.  
- the Ascending Node Longitude is the angle (in radians) from where the body will cross from going south to going north.  
- the Inclination is the tilt of the orbit.  

## Technical Details

- The Integrator used in this simulation is Velocity Verlet.
- The Barnes-Hut Algorithm plays a role in making the simulation more performant by making the physics calculations O(n log n) instead of O(n<sup>2</sup>).
- Double Precision was used instead of float because of Small Precision errors accumulating over time, causing a big error. Double Precision also has a bigger range for massive force (10<sup>-308</sup> to 10<sup>308</sup>) while float has 10<sup>-38</sup> to 10<sup>38</sup>

## Roadmap

### Major:  

 - Burst Compilation  
 - C# Job Integration for Multithreading  
 - Delinking Body from MonoBehaviour  
 - Delinking Simulation from Time.TimeScale  
 - Custom Mesh Generation for Orbit Trails
 - Reliable Close Encounter Handling (Smoothing, Collisions)

#### Low-Priority Major:

 - Fragmentation Physics  
 - Planetesimal-Based Force Calculations  

### Minor:

- Floating Origin for Flexibility
- Non-Singleton Propagator for Multiple Simulations / Scene
- Major Event Triggers (Body Collisions, Orbit Escape, etc.)
- Improved Editor
- Rigidbody and Custom Integrator User Choice
- Multiple Integrators (Velocity Verlet, 4th Order Runge-Kutta, Symplectic Euler, Leapfrog)
- Unity.Mathematics replacing DVector3

## Credits

[Barnes-Hut Algorithm Documentation on arborjs.org](https://arborjs.org/docs/barnes-hut) <br>
[Serway, R. A., & Faughn, J. S. HMH Physics: Student Edition 2017](https://www.amazon.com/Hmh-Physics-Raymond-Ph-D-Serway/dp/0544817737/ref=sr_1_1?crid=3GP9HK833QZHZ&dib=eyJ2IjoiMSJ9.xnCjaAhU1VPa4l1mS96RoP3XsfSu9nxTdhnTNCWF6QUaMJYwN0QNaB1ABuNd4A5j571R8uZnRfqs6a3nzAy1j7J9L1OHGrk6tNSdWVLp7BlsByVX8BXjarmj4nHKWERoZ93oRMOv3JImF1bFQj9AlqEUhh4cvFRxdk0pZS7mYug.tR8r5-vapDorupoqTJktYwHwzhx143McnFvlMh_-cIQ&dib_tag=se&keywords=HMH+Physics%3A+Student+Edition+2017.&qid=1747758685&sprefix=hmh+physics+student+edition+2017.%2Caps%2C241&sr=8-1) <br>
[Arcane Algorithm Archive's Verlet Integration Documentation](https://www.algorithm-archive.org/contents/verlet_integration/verlet_integration.html)  
[Orbital Mechanics - Wikipedia](https://en.wikipedia.org/wiki/Orbital_mechanics)  
[Perifocal coordinate system - Wikipedia](https://en.wikipedia.org/wiki/Perifocal_coordinate_system)
