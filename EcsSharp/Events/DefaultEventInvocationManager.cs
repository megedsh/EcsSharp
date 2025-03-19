using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EcsSharp.Events;

public class DefaultEventInvocationManager : IEventInvocationManager
{
    public Task Invoke(ICollection<EventArgsAndDelegatePair> pairs)
    {
        Task[] array = pairs.SelectMany(fireAllEvents).ToArray();
        return Task.WhenAll(array);
    }

    private IEnumerable<Task> fireAllEvents(EventArgsAndDelegatePair eventArgsAndDelegatePair)
    {
        IEnumerable<DelegateWrapper> delegatesForType = eventArgsAndDelegatePair.DelegatesForType;
        object eventArgs = eventArgsAndDelegatePair.EventArgs;
        return delegatesForType.Select(d => Task.Run(() => d.Invoke(eventArgs)));
    }
}