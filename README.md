# unity-n-body
N-Body Simulation implemented in Unity currently in development.<br>
It is an experimental project of mine that i expect to keep updating for about the next 2 years.
## Features
- Gravitational N-Body Simulation<br>
- Double Precision Mathematics<br>
- Barnes-Hut Algorithm with Adaptive (and static) Simulation Bounds<br>
- Custom Unity Inspector UI<br>
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
- Adaptive Simulation bounds determines if the Simulation bounds will move to accomodate the bodies for accurate calculations with barnes-hut. As of Version 0.2.0, it is better to disable this as it can consume many resources if a body goes flying due to unforeseen bugs.
- Bounds Padding is a extra distance added to the simulation bounds for extra stability with Adaptive Simulation Bounds. Generally not needed on static simuation bounds.
- Simulation Bounds only appears when Adaptive Simulation Bounds is toggled off, and it lets you define a specific domain for your simulation. If bodies go outside this range, physics calculations may not be accurate.

**NBody Properties**

- Mass is the mass of the object (in kg)... i don't know what else to say.
- Initial Velocity is the velocity of the object at the beginning of the simulation.

## Technical Details

- The Integrator used in this simulation is Velocity Verlet.
- The Barnes-Hut Algorithm plays a role in making the simulation more performant by making the physics calculations O(n log n) instead of O(n<sup>2</sup>).
- Double Precision was used instead of float because of Small Precision errors accumulating over time, causing a big error. Double Precision also has a bigger range for massive force (10<sup>-308</sup> to 10<sup>308</sup>) while float has 10<sup>-38</sup> to 10<sup>38</sup>

## Roadmap

- Make the simulation **NOT** tied to the origin of the world to help with integration into games (if that were to happen) and help with adaptive simulation bounds effectiveness.
- Introducting Higher-Order Integrations like Fourth Order Runge-Kutta.
- Visualization Improvements (Orbit trails, Body Scaling, Recording Data)
- Adjustable Simulation Speed separate from Unity's TimeScale
- Integration into Unity DOTS for performance.

## Credits

[Barnes-Hut Algorithm Documentation on arborjs.org](https://arborjs.org/docs/barnes-hut) <br>
[Serway, R. A., & Faughn, J. S. HMH Physics: Student Edition 2017](https://www.amazon.com/Hmh-Physics-Raymond-Ph-D-Serway/dp/0544817737/ref=sr_1_1?crid=3GP9HK833QZHZ&dib=eyJ2IjoiMSJ9.xnCjaAhU1VPa4l1mS96RoP3XsfSu9nxTdhnTNCWF6QUaMJYwN0QNaB1ABuNd4A5j571R8uZnRfqs6a3nzAy1j7J9L1OHGrk6tNSdWVLp7BlsByVX8BXjarmj4nHKWERoZ93oRMOv3JImF1bFQj9AlqEUhh4cvFRxdk0pZS7mYug.tR8r5-vapDorupoqTJktYwHwzhx143McnFvlMh_-cIQ&dib_tag=se&keywords=HMH+Physics%3A+Student+Edition+2017.&qid=1747758685&sprefix=hmh+physics+student+edition+2017.%2Caps%2C241&sr=8-1)
[Arcane Algorithm Archive's Verlet Integration Doncumentation](https://www.algorithm-archive.org/contents/verlet_integration/verlet_integration.html)
