using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcsSharp.Events
{
    public interface IEventInvocationManager
    {
        Task Invoke(ICollection<EventArgsAndDelegatePair> pairs);
    }
}