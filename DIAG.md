# Diagnostics
This file contains tests I ran on najm.dot that relate to accuracy, efficiency, and speed.
Tests on older versions will be lower down the file.

## 0.5.2-alpha1 / 6000.2.2f1 (DirectX 12)
### Hardware:  
- AMD Ryzen 5 5600  
- AMD Wraith Stealth  
- MSI RTX 4060 Ventus 2X OC  
- 16GB DDR4 Dual-Channel SDRAM 1600 MHz (Actual Clock: 1597.1 MHz)  

### Energy Conservation / Barnes-Hut (50,000 Steps)
### Setup:
- Velocity Verlet  
- Distance Multiplier: 1e9 (1 Billion)  
- Physical Multiplier: 6,000,000x  
- Timestep: 0.02 (50Hz) 
<br>

- Bounds: 1024 (Padding: 10)
- Octant Limit: 4096
- Octant Splitting Threshold: 1
- Softening Length2: 0
- Theta: 0 / 0.5 / 1 / 2

### Elements:
#### Sun
Position: (0, 0, 0)  
Mass: 1.989e30

#### Earth
Position: (149.6, 0, 0)  
Mass: 5.9722e24

Keplerian Parameters  
- Inclination: 8.7266463e-7  
- Eccentricity: 0.0167086  
- Argument of Periapsis: 5.0282935749956641  
- Ascending Node Longitude: 3.0525808617380821
- True Anomaly: 4.4865838331795862

_*Rest are automatically set by system_

#### Mars
Position: (229.55, 0, 0)  
Mass: 6.418e23  

Keplerian Parameters
- Inclination: 0.032283180641213917
- Eccentricity: 0.0933941
- Argument of Periapsis: 5.0003129800264654
- Ascending Node Longitude: 0.86497714877
- True Anomaly: 0.41815249155002315

_*Rest are automatically set by system_

### Results
