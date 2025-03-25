namespace EcsSharp.StructComponents;

public class BoolComponent : StructComponent<bool>
{
    public static implicit operator BoolComponent(bool d) =>
        new BoolComponent
        {
            Data = d
        };
}

public abstract class BoolComponent<T> : BoolComponent
    where T : BoolComponent<T>,new()

{
    public static implicit operator bool(BoolComponent<T> input)
    {
        return input.Data;
    }

    public static implicit operator BoolComponent<T>(bool input)
    {
        return input? True:False;
    }

    public static implicit operator BoolComponent<T>(T input)
    {
        return input ? True : False;
    }

    public static implicit operator T(BoolComponent<T> input)
    {
        return (T)input.Data;
    }

    public static T True =>
        new()
        {
            Data = true
        };

    public static T False =>
        new()
        {
            Data = false
        }; 
}