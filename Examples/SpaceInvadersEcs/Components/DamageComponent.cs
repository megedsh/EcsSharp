
using EcsSharp.StructComponents;

namespace SpaceInvadersEcs.Components;

public class DamageComponent : DoubleComponent
{
    public static implicit operator DamageComponent(double d) =>
        new()
        {
            Data = d
        };

    public static implicit operator double(DamageComponent d) => d?.Data ?? 0;

    public static bool operator ==(DamageComponent a, double b)
    {
        double? d = a?.Data;
        return d != null && d.Equals(b);
    }

    public static bool operator !=(DamageComponent a, double b) => !(a == b);

    public override bool Equals(object obj) => base.Equals(obj);

    public override int GetHashCode() => Data.GetHashCode();
}