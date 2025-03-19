using System;
using System.Linq;

namespace EcsSharp.Events.EventArgs;

public abstract class EntityEventArgs<T> where T : ComponentEventArgs
{
    protected EntityEventArgs(IEntity entity, T[] componentEventArgs)
    {
        Entity = entity;
        ComponentEventArgs = componentEventArgs;
        ModifiedDate = componentEventArgs.FirstOrDefault()?.ModifiedDate??DateTime.UtcNow;
    }

    public IEntity Entity { get; }
    public DateTime ModifiedDate { get; }
    public T[] ComponentEventArgs { get; }
}