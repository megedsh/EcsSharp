using System;

namespace EcsSharp.StructComponents;

public interface IDateTimeComponent : IStructComponent<DateTime>
{
}
public abstract class DateTimeComponent: StructComponent<DateTime>, IDateTimeComponent
    
{

}
public class ModifiedTime : DateTimeComponent
{
    public static implicit operator ModifiedTime(DateTime d) =>
        new ModifiedTime
        {
            Data = d
        };
}
public class CreateTime : DateTimeComponent
{
    public static implicit operator CreateTime(DateTime d) =>
        new CreateTime
        {
            Data = d
        };
}