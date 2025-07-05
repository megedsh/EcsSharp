using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EcsSharp.Events.ComponentEvents;
using EcsSharp.Events.EntityEvents;
using EcsSharp.Events.EventArgs;
using EcsSharp.Events.TagEvents;
using EcsSharp.Helpers;
using EcsSharp.Logging;

namespace EcsSharp.Events;

public class EcsEventService : IEcsEventService
{
    private static readonly ICommonLog s_log = CommonLogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType);

    private readonly IEventInvocationManager m_eventInvocationManager;

    public EcsEventService(IEventInvocationManager eventInvocationManager) => m_eventInvocationManager = eventInvocationManager;

    public ComponentCreatedEvents ComponentCreated { get; } = new();
    public ComponentUpdatedEvents ComponentUpdated { get; } = new();
    public ComponentDeletedEvents ComponentDeleted { get; } = new();
    public GlobalCreatedEvent GlobalCreated { get; set; } = new();
    public GlobalUpdatedEvent GlobalUpdated { get; set; } = new();
    public GlobalDeletedEvent GlobalDeleted { get; set; } = new();
    public TagCreatedEvents TaggedEntitiesCreated { get; set; } = new();
    public TagUpdatedEvents TaggedEntitiesUpdated { get; set; } = new();
    public TagDeletedEvents TaggedEntitiesDeleted { get; set; } = new();
    public SpecificCreatedEvent SpecificCreated { get; set; } = new();
    public SpecificUpdatedEvent SpecificUpdated { get; set; } = new();
    public SpecificDeletedEvent SpecificDeleted { get; set; } = new();
    public ITypeFamilyProvider TypeFamilyProvider{get; set; } = new AllInterfacesFamilyProvider();

    public void InvokeEntityCreatedDelegates(EntityCreatedEventArgs[] args)
    {
        IEnumerable<EventArgsAndDelegatePair> stream = Enumerable.Empty<EventArgsAndDelegatePair>();

        if (GlobalCreated.HasDelegates || TaggedEntitiesCreated.HasDelegates || SpecificCreated.HasDelegates)
        {
            if (s_log.IsDebugEnabled)
            {
                foreach (EntityCreatedEventArgs arg in args)
                {
                    s_log.DebugFormat("Entity : {0} Created with {1} components", arg.Entity.Id, arg.CreatedComponents.Length);
                }
            }

            EntitiesCreatedEventArgs col = new(args);
            if (GlobalCreated.HasDelegates)
            {
                EventArgsAndDelegatePair pair = new(col, new DelegateWrapper[] { GlobalCreated });
                stream = stream.Concat(pair.Yield());
            }

            if (TaggedEntitiesCreated.HasDelegates)
            {
                stream = stream.Concat(getTaggedCreatedDelegates(args));
            }

            if (SpecificCreated.HasDelegates)
            {
                stream = stream.Concat(getSpecificCreatedDelegates(args));
            }

            EventArgsAndDelegatePair[] events = stream.ToArray();
            if (events.Length > 0)
            {
                m_eventInvocationManager.Invoke(events);
            }
        }
    }

    public void InvokeComponentUpdatedDelegates(ComponentUpdatedEventArgs[] args)
    {
        IEnumerable<EventArgsAndDelegatePair> stream = Enumerable.Empty<EventArgsAndDelegatePair>();
        foreach (ComponentUpdatedEventArgs arg in args)
        {
            s_log.TraceFormat("Entity : {0}, {1} Component Updated : {2} , Version: {3}", arg.Entity.Id,
                              arg.ComponentType.Name, 
                              arg.Component.Data,
                              arg.Component.Version);


            IEnumerable<Type> typeFamily = TypeFamilyProvider.GetTypeFamily(arg.ComponentType);
            IEnumerable<DelegateWrapper> delegatesForType = ComponentUpdated.GetDelegatesForType(typeFamily);

            EventArgsAndDelegatePair pair = new(arg, delegatesForType);
            stream = stream.Concat(pair.Yield());
        }

        if (TaggedEntitiesUpdated.HasDelegates || GlobalUpdated.HasDelegates || SpecificUpdated.HasDelegates)
        {
            EntitiesUpdatedEventArgs groupedByEntities = groupByEntities(args);

            if (GlobalUpdated.HasDelegates)
            {
                EventArgsAndDelegatePair global = new(groupedByEntities, new DelegateWrapper[] { GlobalUpdated });
                stream = stream.Concat(global.Yield());
            }

            if (SpecificUpdated.HasDelegates)
            {
                ICollection<EventArgsAndDelegatePair> specificDelegates = getSpecificUpdatedDelegates(groupedByEntities);
                stream = stream.Concat(specificDelegates);
            }

            ICollection<EventArgsAndDelegatePair> tagUpdateDelegates = getTaggedUpdatedDelegates(groupedByEntities);

            stream = stream.Concat(tagUpdateDelegates);
        }

        EventArgsAndDelegatePair[] events = stream.ToArray();
        if (events.Length > 0)
        {
            m_eventInvocationManager.Invoke(events);
        }
    }

    public void InvokeComponentCreatedDelegates(ComponentCreatedEventArgs[] args)
    {
        List<EventArgsAndDelegatePair> pairs = new(args.Length);
        foreach (ComponentCreatedEventArgs arg in args)
        {
            s_log.DebugFormat("Entity : {0}, {1} Component Created : {2}", arg.Entity.Id, arg.ComponentType.Name, arg.Component.Data);
            IEnumerable<Type> typeFamily = TypeFamilyProvider.GetTypeFamily(arg.ComponentType);
            IEnumerable<DelegateWrapper> delegatesForType = ComponentCreated.GetDelegatesForType(typeFamily);

            pairs.Add(new EventArgsAndDelegatePair(arg, delegatesForType));
        }

        if (pairs.Count > 0)
        {
            m_eventInvocationManager.Invoke(pairs);
        }
    }

    public void InvokeComponentDeletedDelegates(ComponentDeletedEventArgs[] args)
    {
        IEnumerable<EventArgsAndDelegatePair> stream = Enumerable.Empty<EventArgsAndDelegatePair>();
        foreach (ComponentDeletedEventArgs arg in args)
        {
            s_log.DebugFormat("Entity : {0}, {1} Component Deleted : {2}", arg.Entity.Id, arg.ComponentType.Name, arg.Component.Data);
            IEnumerable<Type> typeFamily = TypeFamilyProvider.GetTypeFamily(arg.ComponentType);
            IEnumerable<DelegateWrapper> delegatesForType = ComponentDeleted.GetDelegatesForType(typeFamily);
            EventArgsAndDelegatePair pair = new(arg, delegatesForType);
            stream = stream.Concat(pair.Yield());
        }

        if (GlobalDeleted.HasDelegates || TaggedEntitiesDeleted.HasDelegates || SpecificDeleted.HasDelegates)
        {
            EntitiesDeletedEventArgs grouped = groupByEntities(args);
            if (GlobalDeleted.HasDelegates)
            {
                EventArgsAndDelegatePair pair = new(grouped, new DelegateWrapper[] { GlobalDeleted });
                stream = stream.Concat(pair.Yield());
            }

            if (TaggedEntitiesDeleted.HasDelegates)
            {
                stream = stream.Concat(getTaggedDeletedDelegates(grouped));
            }

            if (SpecificDeleted.HasDelegates)
            {
                stream = stream.Concat(getSpecificDeletedDelegates(grouped));
            }
        }

        EventArgsAndDelegatePair[] events = stream.ToArray();
        if (events.Length > 0)
        {
            m_eventInvocationManager.Invoke(events);
        }
    }

    private static EntitiesUpdatedEventArgs groupByEntities(ComponentUpdatedEventArgs[] args)
    {
        EntityUpdatedEventArgs[] entityUpdatedArgs = args
                                                     .GroupBy(e => e.Entity)
                                                     .Select(p => new EntityUpdatedEventArgs(p.Key, p.ToArray()))
                                                     .ToArray();
        return new EntitiesUpdatedEventArgs(entityUpdatedArgs);
    }

    private static EntitiesDeletedEventArgs groupByEntities(ComponentDeletedEventArgs[] args)
    {
        EntityDeletedEventArgs[] entityUpdatedArgs = args
                                                     .GroupBy(e => e.Entity)
                                                     .Select(p => new EntityDeletedEventArgs(p.Key, p.ToArray()))
                                                     .ToArray();
        return new EntitiesDeletedEventArgs(entityUpdatedArgs);
    }

    private ICollection<EventArgsAndDelegatePair> getTaggedUpdatedDelegates(EntitiesUpdatedEventArgs grouped)
    {
        if (!TaggedEntitiesUpdated.HasDelegates)
        {
            return Array.Empty<EventArgsAndDelegatePair>();
        }

        Dictionary<string, List<EntityUpdatedEventArgs>> aggregator = new();
        Dictionary<string, DelegateWrapper<EntitiesUpdatedEventArgs>> delegates = new();

        foreach (EntityUpdatedEventArgs entitiesUpdatedEventArg in grouped)
        {
            foreach (string entityTag in entitiesUpdatedEventArg.Entity.Tags)
            {
                DelegateWrapper<EntitiesUpdatedEventArgs> delegatesForTag = TaggedEntitiesUpdated.GetDelegateForTag(entityTag);
                if (delegatesForTag != null)
                {
                    delegates[entityTag] = delegatesForTag;
                    if (!aggregator.TryGetValue(entityTag, out List<EntityUpdatedEventArgs> eventArgs))
                    {
                        eventArgs = new List<EntityUpdatedEventArgs>();
                        aggregator[entityTag] = eventArgs;
                    }

                    eventArgs.Add(entitiesUpdatedEventArg);
                }
            }
        }

        if (aggregator.Count > 0)
        {
            return aggregator.Select(entry =>
                             {
                                 EntitiesUpdatedEventArgs entitiesUpdatedEventArgs = new(entry.Value);
                                 DelegateWrapper<EntitiesUpdatedEventArgs> delegateWrappers = delegates[entry.Key];
                                 return new EventArgsAndDelegatePair(entitiesUpdatedEventArgs, new[] { delegateWrappers });
                             })
                             .ToArray();
        }

        return Array.Empty<EventArgsAndDelegatePair>();
    }

    private ICollection<EventArgsAndDelegatePair> getTaggedCreatedDelegates(EntityCreatedEventArgs[] args)
    {
        Dictionary<string, List<EntityCreatedEventArgs>> aggregator = new();
        Dictionary<string, DelegateWrapper<EntitiesCreatedEventArgs>> delegates = new();

        foreach (EntityCreatedEventArgs entitiesUpdatedEventArg in args)
        {
            foreach (string entityTag in entitiesUpdatedEventArg.Entity.Tags)
            {
                DelegateWrapper<EntitiesCreatedEventArgs> delegatesForTag = TaggedEntitiesCreated.GetDelegateForTag(entityTag);
                if (delegatesForTag != null)
                {
                    delegates[entityTag] = delegatesForTag;
                    if (!aggregator.TryGetValue(entityTag, out List<EntityCreatedEventArgs> eventArgs))
                    {
                        eventArgs = new List<EntityCreatedEventArgs>();
                        aggregator[entityTag] = eventArgs;
                    }

                    eventArgs.Add(entitiesUpdatedEventArg);
                }
            }
        }

        if (aggregator.Count > 0)
        {
            return aggregator.Select(entry =>
                             {
                                 EntitiesCreatedEventArgs eventArgs = new(entry.Value);
                                 DelegateWrapper<EntitiesCreatedEventArgs> delegateWrappers = delegates[entry.Key];
                                 return new EventArgsAndDelegatePair(eventArgs, new[] { delegateWrappers });
                             })
                             .ToArray();
        }

        return Array.Empty<EventArgsAndDelegatePair>();
    }

    private IEnumerable<EventArgsAndDelegatePair> getTaggedDeletedDelegates(EntitiesDeletedEventArgs grouped)
    {
        Dictionary<string, List<EntityDeletedEventArgs>> aggregator = new();
        Dictionary<string, DelegateWrapper<EntitiesDeletedEventArgs>> delegates = new();

        foreach (EntityDeletedEventArgs entitiesUpdatedEventArg in grouped)
        {
            foreach (string entityTag in entitiesUpdatedEventArg.Entity.Tags)
            {
                DelegateWrapper<EntitiesDeletedEventArgs> delegatesForTag = TaggedEntitiesDeleted.GetDelegateForTag(entityTag);
                if (delegatesForTag != null)
                {
                    delegates[entityTag] = delegatesForTag;
                    if (!aggregator.TryGetValue(entityTag, out List<EntityDeletedEventArgs> eventArgs))
                    {
                        eventArgs = new List<EntityDeletedEventArgs>();
                        aggregator[entityTag] = eventArgs;
                    }

                    eventArgs.Add(entitiesUpdatedEventArg);
                }
            }
        }

        if (aggregator.Count > 0)
        {
            return aggregator.Select(entry =>
                             {
                                 EntitiesDeletedEventArgs eventArgs = new(entry.Value);
                                 DelegateWrapper<EntitiesDeletedEventArgs> delegateWrappers = delegates[entry.Key];
                                 return new EventArgsAndDelegatePair(eventArgs, new[] { delegateWrappers });
                             })
                             .ToArray();
        }

        return Array.Empty<EventArgsAndDelegatePair>();
    }

    private IEnumerable<EventArgsAndDelegatePair> getSpecificCreatedDelegates(EntityCreatedEventArgs[] args)
    {
        return args.Select(eventArgs =>
                   {
                       DelegateWrapper<EntityCreatedEventArgs> delegateForId = SpecificCreated.GetDelegateForId(eventArgs.Entity.Id);
                       if (delegateForId != null)
                       {
                           return new EventArgsAndDelegatePair(eventArgs, new[] { delegateForId });
                       }

                       return null;
                   })
                   .Where(d => d != null)
                   .ToArray();
    }

    private ICollection<EventArgsAndDelegatePair> getSpecificUpdatedDelegates(EntitiesUpdatedEventArgs groupedByEntities)
    {
        return groupedByEntities.Select(eventArgs =>
                                {
                                    DelegateWrapper<EntityUpdatedEventArgs> delegateForId = SpecificUpdated.GetDelegateForId(eventArgs.Entity.Id);
                                    if (delegateForId != null)
                                    {
                                        return new EventArgsAndDelegatePair(eventArgs, new[] { delegateForId });
                                    }

                                    return null;
                                })
                                .Where(d => d != null)
                                .ToArray();
    }

    private IEnumerable<EventArgsAndDelegatePair> getSpecificDeletedDelegates(EntitiesDeletedEventArgs grouped)
    {
        return grouped.Select(eventArgs =>
                      {
                          DelegateWrapper<EntityDeletedEventArgs> delegateForId = SpecificDeleted.GetDelegateForId(eventArgs.Entity.Id);
                          if (delegateForId != null)
                          {
                              return new EventArgsAndDelegatePair(eventArgs, new[] { delegateForId });
                          }

                          return null;
                      })
                      .Where(d => d != null)
                      .ToArray();
    }
}