using System.Collections.Generic;

namespace EcsSharp.Events
{
    public class EventArgsAndDelegatePair
    {
        public object EventArgs { get; }
        public IEnumerable<DelegateWrapper> DelegatesForType { get; }
        

        public EventArgsAndDelegatePair(object eventArgs,
                                        IEnumerable<DelegateWrapper> delegatesForType)
        {
            EventArgs = eventArgs;
            DelegatesForType = new List<DelegateWrapper>(delegatesForType);
        }
    }
}