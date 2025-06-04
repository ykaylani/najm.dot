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
git clone https://github.com/sandalconsumer/unity-n-body.git
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

1. N Body Originator
2. N Body <br>

- The **Originator** controls all the N-Bodies' movement and physics, and the **N-Bodies** are for storing the individual data of each body.

**Originator Properties**

General
- Dist Multiplier controls the **Scale** of the simulation. The default value is 1 billion, meaning that every 1 unity meter is 1 billion simulation meters.
- Simulation Timestep controls the amount of time the objects will move then they are updated. If the value is "0.05", the bodies will move by a time of 0.05 in-world seconds every time unity's FixedUpdate is called (every 0.02 seconds by default) <br>

Barnes-Hut
- the Opening Angle Criterion is the threshold that the Barnes-Hut algorithm will use to determine if it should use an approximation of mass or take the exact masses. values near 0 cause more accuracy but less prformance, and the opposite is true for values near 1. <br>

Bounds
- Adaptive Simulation bounds determines if the Simulation bounds will move to accomodate the bodies for accurate calculations with barnes-hut. As of Version 0.3.1, it is better to disable IF you are using basic initial velocities as they have to chance to fling bodies thousands of meters away, and that causes the bounds to try and follow them.
- Bounds Padding is a extra distance added to the simulation bounds for extra stability with Adaptive Simulation Bounds. Generally not needed when using static simuation bounds.
- Simulation Bounds only appears when Adaptive Simulation Bounds is toggled off, and it lets you define a specific domain for your simulation. If bodies go outside this range, physics calculations may not be accurate.

Visualization  
- Orbit Trail Material is the material the orbit visualization will use. This will be used as the default for all trails, but individual materials can be set from the N-Bodies.
- Orbit Width controls the width of the line that represents the orbit trail.
- Vizualization Timestep is how much time it will take to update the visualization.

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

Visualization
- Orbit Trails toggles the visibility of orbit trails. This is currently not recommended for purely performance-based applications as it uses Unity's built-in LineRenderer, which can produce a ton of CPU overhead with many objects.  
- Orbit Trail Material will be used to render the path. If the N-Body has this as empty, the simulation will fallback to the Originator's Orbit Trail Material.  
- Orbit trail length defines the maximum number of previous visualization timesteps that can be displayed.  

## Technical Details

- The Integrator used in this simulation is Velocity Verlet.
- The Barnes-Hut Algorithm plays a role in making the simulation more performant by making the physics calculations O(n log n) instead of O(n<sup>2</sup>).
- Double Precision was used instead of float because of Small Precision errors accumulating over time, causing a big error. Double Precision also has a bigger range for massive force (10<sup>-308</sup> to 10<sup>308</sup>) while float has 10<sup>-38</sup> to 10<sup>38</sup>

## Roadmap

The roadmap for this project in chronological order.
- Introducting Higher-Order Integrations like Fourth Order Runge-Kutta.
- Object Pooling for the Barnes-Hut algorithm's octants to decrease GC pressure.
- Make the simulation **NOT** tied to the origin of the world to help with integration into games (if that were to happen) and help with adaptive simulation bounds' effectiveness.
- Custom Mesh Generation for visualization replacing LineRenderer as LineRenderer creates CPU overhead at scale.
- Adjustable Simulation Speed separate from Unity's TimeScale.
- Body Collisions for more stability.
- Data Recording for export.
- Integration into Unity DOTS for performance.

## Credits

[Barnes-Hut Algorithm Documentation on arborjs.org](https://arborjs.org/docs/barnes-hut) <br>
[Serway, R. A., & Faughn, J. S. HMH Physics: Student Edition 2017](https://www.amazon.com/Hmh-Physics-Raymond-Ph-D-Serway/dp/0544817737/ref=sr_1_1?crid=3GP9HK833QZHZ&dib=eyJ2IjoiMSJ9.xnCjaAhU1VPa4l1mS96RoP3XsfSu9nxTdhnTNCWF6QUaMJYwN0QNaB1ABuNd4A5j571R8uZnRfqs6a3nzAy1j7J9L1OHGrk6tNSdWVLp7BlsByVX8BXjarmj4nHKWERoZ93oRMOv3JImF1bFQj9AlqEUhh4cvFRxdk0pZS7mYug.tR8r5-vapDorupoqTJktYwHwzhx143McnFvlMh_-cIQ&dib_tag=se&keywords=HMH+Physics%3A+Student+Edition+2017.&qid=1747758685&sprefix=hmh+physics+student+edition+2017.%2Caps%2C241&sr=8-1) <br>
[Arcane Algorithm Archive's Verlet Integration Documentation](https://www.algorithm-archive.org/contents/verlet_integration/verlet_integration.html)  
[Orbital Mechanics - Wikipedia](https://en.wikipedia.org/wiki/Orbital_mechanics)  
[Perifocal coordinate system - Wikipedia](https://en.wikipedia.org/wiki/Perifocal_coordinate_system)
