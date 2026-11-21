using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EcsSharp.Helpers.Synchronizer;

namespace EcsSharp.Events;

public class DefaultEventInvocationManager : IEventInvocationManager
{
    private readonly TplSynchronizer m_invocationSynchronizer;

    public DefaultEventInvocationManager()
    {
        SynchronizerConfig synchronizerConfig = new SynchronizerConfigBuilder()
                                                .WithId("EcsEventInvocation")
                                                .Build();
        m_invocationSynchronizer = new TplSynchronizer(synchronizerConfig);
    }

    public Task Invoke(ICollection<EventArgsAndDelegatePair> pairs)
    {
        Task[] array = pairs.SelectMany(fireAllEvents).ToArray();
        return Task.WhenAll(array);
    }

    private IEnumerable<Task> fireAllEvents(EventArgsAndDelegatePair eventArgsAndDelegatePair)
    {
        IEnumerable<DelegateWrapper> delegatesForType = eventArgsAndDelegatePair.DelegatesForType;
        object eventArgs = eventArgsAndDelegatePair.EventArgs;
        return delegatesForType.Select(d => m_invocationSynchronizer.QueueInvocation(() => d.Invoke(eventArgs)));
    }
}