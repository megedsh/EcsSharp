namespace EcsSharp.StructComponents;

public class BoolComponent : StructComponent<bool>
{
    public static implicit operator BoolComponent(bool d) =>
        new BoolComponent
        {
            Data = d
        };
}