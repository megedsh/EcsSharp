using System;
using EcsSharp.Events;
using EcsSharp.Storage;

namespace EcsSharp
{
    public interface IEcsRepoFactory
    {
        IEcsRepo Create(string name = null);
    }

    public class DefaultEcsRepoFactory : IEcsRepoFactory
    {
        public IEcsRepo Create(string name = null)
        {
            IEventInvocationManager invocationManager = new DefaultEventInvocationManager();
            IEcsEventService eventService = new EcsEventService(invocationManager);
            IEcsStorage storage = new EcsStorage();
            return new EcsRepo(name??Guid.NewGuid().ToString(), storage, eventService);
        }
    }
}