using System;

namespace EcsSharp.Tests;

public interface ICar
{
    string Id { get; }
}
public class Sedan : ICar
{
    public Sedan(string id="") => Id = id;

    public string Id { get; }

    public override string ToString() => $"{nameof(Id)}: {Id}";
}
public class Suv : ICar
{
    public Suv(string id ="") => Id = id;

    public string Id { get;  }

    public override string ToString() => $"{nameof(Id)}: {Id}";
}
public class Location
{
    public Location(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    public double X { get;  }
    public double Y { get;  }
    public double Z { get;  }

    public bool Equals(Location other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != GetType())
        {
            return false;
        }

        return Equals((Location)obj);
    }

    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public override string ToString() => $"{nameof(X)}: {X}, {nameof(Y)}: {Y}, {nameof(Z)}: {Z}";
}