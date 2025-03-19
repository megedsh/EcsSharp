namespace EcsSharp
{
    public delegate bool ComponentSetCondition<T>(T oldComponent, T newComponent);
}