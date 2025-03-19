namespace EcsSharp.StructComponents;

public class StringComponent : StructComponent<string>
{
    public static implicit operator StringComponent(string d) =>
        new StringComponent
        {
            Data = d
        };
}