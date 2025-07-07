using System;
using System.Collections.Generic;

namespace EcsSharp;

public interface IEntity
{
    string Id { get; }
    string[] Tags { get; }
    T           GetComponent<T>();
    T[]         GetComponents<T>();
    Component   GetComponent(Type  componentType);
    Component[] GetComponents(Type componentType);
    Component[] GetAllComponents();
    T           RefreshComponent<T>();
    T[]         RefreshComponents<T>();
    Component   RefreshComponent(Type  componentType);
    Component[] RefreshComponents(Type componentType);
    Component[] RefreshAllComponents();
    IEntity     SetComponent<T>(T             component);
    IEntity     SetComponents(params object[] components);
    IEntity     SetWithVersion<T>(T           component, ulong version);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="component"></param>
    /// <param name="condition"></param>
    /// <param name="setWhenNotExist"></param>
    /// <returns>returns 'true' if component was updated</returns>
    bool ConditionalSet<T>(T component, ComponentSetCondition<T> condition, bool setWhenNotExist = true);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="condition"></param>
    /// <param name="componentFactory"></param>
    /// <param name="setWhenNotExist"></param>
    /// <returns>returns 'true' if component was updated</returns>
    bool ConditionalSet<T>(Predicate<T> condition, Func<T, T> componentFactory, bool setWhenNotExist = true);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="component"></param>
    /// <returns>returns 'true' if component was updated</returns>
    bool SetWhenNotEqual<T>(T component);

    /// <summary>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="componentFactory"></param>
    /// <param name="setWhenNotExist"></param>
    /// <returns>returns 'true' if component was updated</returns>
    bool SetWhenNotEqual<T>(Func<T, T> componentFactory, bool setWhenNotExist = true);

    IEntity UpdateComponent<T>(Func<T, T> componentFactory, bool setWhenNotExist = true);
    bool    HasComponent<T>();
    bool    HasComponent(Type type);

    IEntity AddTag(params    string[] tag);
    bool    HasTag(params    string[] tag);
    bool    HasAnyTag(params string[] tag);

    Component[] CachedComponents { get; }
    bool    Exists();
    IEntity Clone();
}
internal interface IEntityInternal
{
    void SetComponentsInEntityCache(params IEnumerable<Component> components);
}