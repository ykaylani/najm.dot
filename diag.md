# Diagnostics
This file contains tests I ran on najm.dot that relate to accuracy, efficiency, and speed.
Tests on older versions will be lower down the file.

## 0.5.2-alpha1 / 6000.2.2f1 (DirectX 12)
### Hardware:  
- AMD Ryzen 5 5600  
- AMD Wraith Stealth  
- MSI RTX 4060 Ventus 2X OC  
- 16GB DDR4 Dual-Channel SDRAM 1600 MHz (Actual Clock: 1597.1 MHz)  

### Energy Conservation (50,000 Steps)
### Setup:
- Semi-Implicit Euler / Velocity Verlet  
- Distance Multiplier: 1e9 (1 Billion)  
- Physical Multiplier: 6,000,000x  
- Timestep: 0.01 (100Hz) 
<br>

- Bounds: 1000 (Padding: 10)
- Octant Limit: 65536
- Octant Splitting Threshold: 1
- Softening Length<sup>2</sup>: 0
- Theta: 0

### Elements:
#### Sun
Position: (0, 0, 0)  
Mass: 1.989e30

#### Earth
Position: (0, 0, 149.6)  
Mass: 5.9722e24

Keplerian Parameters  
- Inclination: 8.7266463e-7  
- Eccentricity: 0.0167086  
- Argument of Periapsis: 5.0282935749956641  
- Ascending Node Longitude: 3.0525808617380821

Rest are automatically set by system  

### Results
#### Run 1 / Semi-Implicit Euler:
Secular Drift (std): 16.09 ppm  
Oscillatory Drift (std): 1966.9 ppm  

#### Run 1 / Velocity Verlet:  
Secular Drift (std): 0.1222 ppm  
Oscillatory Drift (std): 12.69 ppm  
Note: Index 0 Removed (Outlier)  
