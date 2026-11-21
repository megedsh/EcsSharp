namespace EcsSharp.StructComponents;

public abstract class StringComponent : StructComponent<string>
{
}

public abstract class StringComponent<T> : StringComponent
    where T : StringComponent<T>, new()

{
    public static implicit operator StringComponent<T>(string d) =>
        new T
        {
            Data = d
        };

    public static implicit operator string(StringComponent<T> d) => d.Data;
    public static implicit operator T(StringComponent<T>      d) => (T)d.Data;
    public static implicit operator StringComponent<T>(T   d) => d;
}
