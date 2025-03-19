using EcsSharp.StructComponents;

namespace SpaceInvadersEcs.Components;

public class InvaderCount : Int32Component
{
    public static implicit operator InvaderCount(int val) =>
        new()
        {
            Data = val
        };

    public static implicit operator double(InvaderCount val) => val?.Data ?? 0;

    public static bool operator ==(InvaderCount a, int b)
    {
        double? d = a?.Data;
        return d != null && d.Equals(b);
    }

    public static bool operator !=(InvaderCount a, int b) => !(a == b);
}