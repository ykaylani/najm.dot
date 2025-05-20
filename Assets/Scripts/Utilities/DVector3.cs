using UnityEngine;

[System.Serializable]
public struct DVector3
{
    public double x;
    public double y;
    public double z;

    public DVector3(double x, double y, double z)
    {
        this.x = x;
        this.y = y; 
        this.z = z;
    }

    public static DVector3 operator+(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x + b.x, a.y + b.y, a.z + b.z);
    }

    public static DVector3 operator-(DVector3 a, DVector3 b)
    {
        return new DVector3(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    
    public static DVector3 operator*(DVector3 a, double b)
    {
        return new DVector3(a.x * b, a.y * b, a.z * b);
    }

    public static DVector3 operator /(DVector3 a, double b)
    {
        return new DVector3(a.x / b, a.y / b, a.z / b);
    }
    
    public static bool operator !=(DVector3 a, DVector3 b)
    {
        if (a.x != b.x || a.y != b.y || a.z != b.z)
        {
            return true;
        }
        
        return false;
    }

    public static bool operator ==(DVector3 a, DVector3 b)
    {
        if (a.x == b.x && a.y == b.y && a.z == b.z)
        {
            return true;
        }
        
        return false;
    }

    public double magnitude
    {
        get
        {
            return System.Math.Sqrt(this.x * this.x + this.y * this.y + this.z * this.z);
        }
    }

    public double sqrMagnitude
    {
        get
        {
            return this.x * this.x + this.y * this.y + this.z * this.z;
        }
    }

    public DVector3 normalized
    {
        get
        {
            if (magnitude <= 1e-6)
            {
                return new DVector3(0, 0, 0);
            }
        
            return new DVector3(this.x / magnitude, this.y / magnitude, this.z / magnitude);
        }
    }

    public static DVector3 zero
    {
        get
        {
            return new DVector3(0, 0, 0);
        }
    }

    public static implicit operator DVector3(Vector3 a)
    {
        return new DVector3(a.x, a.y, a.z);
    }
    
    public static implicit operator Vector3(DVector3 a)
    {
        return new Vector3((float)a.x, (float)a.y, (float)a.z);
    }
    
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }
}
