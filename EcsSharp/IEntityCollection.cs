using System.Collections.Generic;

namespace EcsSharp
{
    public interface IEntityCollection : IReadOnlyList<IEntity>
    {
        IEntityCollection Clone();
    }
}