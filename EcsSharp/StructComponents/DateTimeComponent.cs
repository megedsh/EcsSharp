using System;

namespace EcsSharp.StructComponents;

public interface IDateTimeComponent
{
}
public abstract class DateTimeComponent : StructComponent<DateTime>, IDateTimeComponent
{
}
public abstract class DateTimeComponent<T> : DateTimeComponent
    where T : DateTimeComponent<T>, new()

{
    public static explicit operator DateTimeComponent<T>(DateTime d) =>
        new T
        {
            Data = d
        };

    public static implicit operator DateTime(DateTimeComponent<T> d) => d.Data;
    public static implicit operator T(DateTimeComponent<T>        d) => (T)d.Data;
    public static implicit operator DateTimeComponent<T>(T        d) => d;
}
public class ModifiedTime : DateTimeComponent<ModifiedTime>
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