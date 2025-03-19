using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcsSharp.Events;

// using this invocation manager might invoke events not in the order that they happened.
public class AsyncEventInvocationManager : IEventInvocationManager
{
    public Task Invoke(ICollection<EventArgsAndDelegatePair> pairs)
    {
        return Task.Run(() =>
            {

                foreach (EventArgsAndDelegatePair eventArgsAndDelegatePair in pairs)
                {
                    IEnumerable<DelegateWrapper> delegatesForType = eventArgsAndDelegatePair.DelegatesForType;
                    object eventArgs = eventArgsAndDelegatePair.EventArgs;
                    foreach (DelegateWrapper d in delegatesForType)
                    {
                        d.Invoke(eventArgs);
                    }
                }
            }
        );
    }
}