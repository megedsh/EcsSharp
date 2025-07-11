[![License](https://img.shields.io/github/license/megedsh/EcsSharp.svg)](https://github.com/megedsh/EcsSharp/blob/master/LICENSE.txt) 
[![NuGet](https://img.shields.io/nuget/v/EcsSharp.svg)](https://nuget.org/packages/EcsSharp) 

<img src="EcsSharpIcon.png" width="300">

# Introduction 

The Entity Component System (ECS) is the core of Data-Oriented Tech Stack. As the name indicates, ECS has three principal parts:

* **Entities** — the entities, or things, that populate your program.
* **Components** — the data associated with your entities, but organized by the data itself rather than by entity. (This difference in organization is one of the key differences between an object-oriented and a data-oriented design.)
* **Systems** — the logic that transforms the component data from its current state to its next state— for example, a system might update the positions of all moving entities by their velocity times the time interval since the previous frame.

# Resources
- [Resource](https://www.google.com/search?q=entity+component+system+architecture&oq=entity+component+system+architecture&gs_lcrp=EgZjaHJvbWUyCQgAEEUYORiABDIICAEQABgWGB4yCAgCEAAYFhgeMg0IAxAAGIYDGIAEGIoFMg0IBBAAGIYDGIAEGIoFMg0IBRAAGIYDGIAEGIoFMgYIBhBFGDwyBggHEEUYPNIBCDEyMjNqMGo3qAIAsAIA&sourceid=chrome&ie=UTF-8)
- [ECS FAQ](https://github.com/SanderMertens/ecs-faq)

# Why EcsSharp ?
I built this repository because the existing ones did not have the features that I needed. 
* Events - Knowing when an entity/component has changed or deleted. Yes, I know it is an anti-pattern. I wanted it anyway.
* Tags - Adding tags to entities makes it less complicated to query. Yes, another anti-pattern. 
* Quering by component interfaces.
* Change collectors - based on events

# Getting Started
Add EcsSharp dependency from Nuget

```cli
nuget install EcsSharp
```

Create an Ecs Repository - you are not limited to one repository. for small applications one is enough

``` csharp
IEcsRepo ecsRepo = new DefaultEcsRepoFactory().Create();
```

# Basic Ecs operations

The examples below use the following classes:
``` csharp
public interface ICar
{
    string Id { get; }
    // Equals and Hashcode
}
public class Sedan : ICar
{
    public Sedan(string id="") => Id = id;
    public string Id { get; }
    // Equals and Hashcode
}
public class Suv : ICar
{
    public Suv(string id ="") => Id = id;
    public string Id { get;  }
    // Equals and Hashcode
}
public class Location
{
    public Location(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    public double X { get;  }
    public double Y { get;  }
    public double Z { get;  }
    // Equals and Hashcode
}
```


## Creating and setting components

Create using the entity builder

```csharp
Sedan sedan = new Sedan();
repo.EntityBuilder().WithComponents(sedan).Build();
```

Create then set
```csharp
IEntity entity1 = repo.Create();
Sedan sedan = new Sedan();
entity1.SetComponent(sedan);
```

Create with components
```csharp
Sedan car = new Sedan("A");
Location location = new Location(1, 1, 1);
IEntity entity1 = repo.Create(car, location);
```

An entity can contain only one instance of the same type
```csharp
Sedan sedanA = new Sedan("A");
Sedan sedanB = new Sedan("B");
Suv suvA = new Suv("A");
IEntity entity1 = repo.Create(sedanA); // entity will be created with sedanA as a component
entity1.SetComponent(sedanB); // the SedanA component will be replaced with SedanB component
entity1.SetComponent(suvA); // suvA will be added to the entity, which will now contain 2 components
```

## Component Interfaces
By default, all components added could also be queried by their interface.
This option can be overridden by setting a custom ITypeFamilyProvider to the repo and the events 
when creating a new component, only the interface types provided by the TypeFamilyProvider will be available for quering
```csharp
ExplicitInterfacesFamilyProvider tfp = new ExplicitInterfacesFamilyProvider();
tfp.Add(typeof(Sedan), typeof(ICar));
tfp.Add(typeof(Suv),   typeof(ICar));


IEventInvocationManager invocationManager = new DefaultEventInvocationManager();
IEcsEventService eventService = new EcsEventService(invocationManager)
{
  TypeFamilyProvider = tfp
};
IEcsStorage storage = new EcsStorage
{
  TypeFamilyProvider = tfp
};
return new EcsRepo("test", storage, eventService);
```

## Tagging entities
Simple string tags can be used to mark the entity for easier retrieval and deletion.
The entities are indexd by their tags and most functionality allows filtering by these tags.
A component containing these tags is created and added to the entity by default.

Create and Tag using the entity builder

```csharp
Sedan sedan = new Sedan();
repo.EntityBuilder().WithComponents(sedan).WithTags("foo").Build();
```

```csharp
IEntity entity1 = repo.Create();
entity1.AddTag("foo","bar")
```

## Deleting entities
Deleting specific component from the entity is not supported.
Only deleting the entire entitiy is possible
```csharp
repo.Delete(entity1); // deletes an entity
repo.Delete("aaa-bbb-ccc"); // Deletes an entity with a specific id

repo.DeleteEntitiesByComponent<ICar>(); // Will delete all entities that contain a component that inherits from ICar
repo.DeleteEntitiesByComponent<ICar>(c=>c.Id=="A"); // Will delete all entities that contain a component that inherits from ICar, and match the predicate 
repo.DeleteEntitiesByComponent<ICar>(c=>c.Id=="A", new[]{"foo"}); // Will delete all entities that contain a component that inherits from ICar,match the predicate, and containt the tag "foo"

repo.DeleteEntitiesWithTag("foo","bar") // deletes any entity that are tagged with "foo" or(!) "bar"

```

# Queries
## Single entity queries
Using the QuerySingle function will throw an exception if more than one result fitting the filter was found.
If a single entity matched it will be returned, If no entity matched the result will be null.
Most query functions have an overload that accepts tags as an additional filter

```csharp
repo.QuerySingle<Sedan>(); // returns an entity that has a component of type Sedan on it
repo.QuerySingle<Sedan>(c => c.Id == "A") // returns an entity that has a component of type Sedan on it and matches the predicate
repo.QuerySingle<ICar>((car, entity) => entity.HasComponent<Location>() && c.Id == "B"); // returns an entity that has a component of type ICar on it and matches the entity and component predicate
repo.QuerySingle<ICar>("foo","bar") // returns an entity that has a component of type ICar on it and is tagged with "foo" or "bar"

repo.QuerySingle("aaa-bbb-ccc") // returns an entity with the matching id
repo.QuerySingle(new[] { typeof(Sedan), typeof(Suv) }, e => e.HasComponent<Location>())) // returns an entity that has a components of type Sedan or Suv on it and matches the entity predicate

```

## Query Multiple Entities
The most common queries. these queries will return an entity collection object.
If no entities matching the filter are found, an empty collection will be returned.
Most query functions have an overload that accepts tags as an additional filter

``` csharp
repo.QueryAll(); // will return all entities in the repository
repo.Query<Sedan>(); // returns all entities containing a component of type Sedan
repo.Query<Sedan>(c => c.Id == "A")  // returns all entities containing a component of type Sedan and matches the predicate
repo.Query(new []{typeof(Sedan), typeof(Suv)}); // returns all entities that have either Sedan or Suv Components
```

# Component Cache
When you query an entity by its components, the components are cached in the entity object so accessing them will not query the repo another time.
This also means that the components will reflaect their value at the time of the query.
Any changes in the components done after the query (by another thread or your own) will not be reflected in the entity.
you can however refresh the components - this will access the repo again and retrieve an up-to-date component.
this is also very handy when you keep a reference to an entity and want to retrieve the component value multiple times throughout the entity lifetime


```csharp
Sedan sedanA = new Sedan("A");
Suv suvA = new Suv("A");
Suv suvA = new Suv("B");

repo.Create(sedanA,suvA); // entity will be created

var e = repo.QuerySingle(new []{typeof(Sedan), typeof(Suv)}); // returns the entity with the component values already cached

Suv result;
result = e.GetComponent<Suv>(); // this will return the cached version of suvA;

e.SetComponent(suvB); // this will update the component in the repo to SuvB

result = e.GetComponent<Suv>(); // this will still return the cached version of suvA;

result = e.RefreshComponent<Suv>; // this will access the repository again, and retrieve an up-to-date component SuvB

```

# Update or insert
Use this helper class to create atomic, thread safe 'Upsert' operation on a single entity.
the helper is thread safe and can be cached.

``` csharp
ICreateOrUpdateBuilder builder = repo.CreateOrUpdateBuilder();
IEntity entity1 = builder.Having<Sedan>(car=>car.Id=="A")
        .WhenCreated(e => e.SetComponent(sedan))
        .WhenEither(e => e.SetComponent(location))
        .Run();

// function will query for an entity with a Sedan component that matches the predicate
// if the entity dosent exist the 'WhenCreated' delegate will be invoked
// In both cases (entity exists, entity created), the 'WhenEither' delegate will be invoked
```
Notes:
- Having functions are equivilent to QuerySingle functions
- If more than one entity matches the query, and exception will be thrown
- only one having function can be updated per run.
- the helper instance is locked untill 'Run' function is invoked. Other threads cannot update any members of this class

# Batch update
Use this function to run atomic thread safe batch of repository and entity manipulation commands

```csharp
repo.BatchUpdate(r =>
            {
                IEntity entity = r.Create();
                entity.AddTag("foo");
                entity.SetComponent(new Sedan("A"));
                entity.SetComponent(new Suv("B"));


                IEntity entity2 = r.Create();
                entity2.AddTag("bar");
                entity2.SetComponent(new Sedan("aaa"));
                entity2.SetComponent(new Suv("bbb"));

                r.Delete("aaa-bbb-ccc");
            });
```

# Entity Lookup Buckets
 A lookup bucket enables you to index entities using a custom unique key (usualy a key generated from a component or a component property).
 After a bucket is created, entities matching the creteria can be queried from the bucket using the key, and not from the repository.
 since the lookup is indexed by the key, this will make fetching the entity faster.
 

``` csharp
//Create a bucket - add only components that have an ICar component
// the key is created from the 'Id' property of the ICar component
IEntityLookupBucket<string> bucket = repo.CreateLookupBucket(e => e.HasComponent<ICar>(), e => e.GetComponent<ICar>().Id);

// add 2 entities to the repo
IEntity entity1 = repo.CreateWithComponents(new Sedan("A"));
IEntity entity2 = repo.CreateWithComponents(new Sedan("B"));

// bucket will containt the entity that matches "A"  and "B"
Assert.IsTrue(bucket.Contains("A"));
Assert.IsTrue(bucket.TryGetEntity("B", out IEntity entity));

// Delete one of the entities from the repo
repo.Delete(entity1);
// the entity was removed from the bucket as well
Assert.IsFalse(bucket.Contains("A"));
``` 
Notes:
- When creating the bucket the repo will scan all the avilable entities and populate the bucket with matches
- The buket key factory function expects a unique result. If multple entities return the same key, the bucket will contain only one entity, withou promiss of integrity or insertation order


# Entity functions

```csharp
IEntity e = repo.QuerySingle<ICar>();

e.SetComponents(new Sedan("A"), new Location(1,2,3)); // sets multiple components;
e.ConditionalSet(new Sedan("B"), (oldComp, newComp) => (oldComp.Id=="A")); // sets a component after checking the predicate with the current component (on not exists stratagy by parameter)
e.SetWhenNotEqual(car); // Will set the component only if current component equality comparare returns false;

Type[] ct = e.ComponentTypes; // returns an array of types currently set to the entity
ICar car = e.GetComponent<ICar>(); // returns the component of type Icar, if more than one component exists (posible when quering an interface) then an exception is thrown
ICar[] carS = e.GetComponentS<ICar>(); // returns all components of type Icar currently set to the entity
bool b = e.HasComponent<Location>(); //returns the existance of a component;
Component[] components = e.GetAllComponents(); // returns all the components (in their container wrapper) of the entity
Sedan sedan =  e.RefreshComponent<Sedan>; // this will access the repository again, and retrieve an up-to-date component

e.AddTag("foo","bar"); // add tags to the entity
e.HasTag("foo"); // returns the existance of a tag

```

# Repository Events
When an entity or a component is update, you can recieve an event with the details of the change.

- There are several types of events you can register to.
- All events are invoked in a new thread.
- Batch modifications will result in a single event with aggregated event arguments on all the changes made.

## Global events
```csharp
repo.Events.GlobalCreated += args => doOnEntityCreated(args);  // invoked when any entity is created
repo.Events.GlobalUpdated += args => doOnEntityUpdated(args);  // invoked when any entity is updated (includes created entities)
repo.Events.GlobalDeleted += args => doOnEntityDeleted(args);  // invoked when any entity is deleted
```

## Component events

```csharp
repo.Events.ComponentCreated[typeof(ICar)] += args => doOnCarCreated(args);  // invoked when a component of type ICar is created
repo.Events.ComponentCreated[typeof(ICar)] += args =>  doOnCarUpdated(args); // invoked when a component of type ICar is updated (includes created components)
repo.Events.ComponentUpdated[typeof(ICar)] += args =>  doOnCarDeleted(args); // invoked when a component of type ICar is deleted
```

## Tagged Entities events

```csharp
repo.Events.TaggedEntitiesCreated["foo"] += args => doOnEntityCreated(args); // invoked when an entity tagged with "foo" is created
repo.Events.TaggedEntitiesUpdated["foo"] += args => doOnEntityUpdated(args); // invoked when an entity tagged with "foo" is updated (includes created entities)
repo.Events.TaggedEntitiesDeleted["foo"] += args => doOnEntityDeleted(args); // invoked when an entity tagged with "foo" is deleted

```

# Component changes collector
Use this feature to collect change and deletes of repository entities. Usually useful when distribution is needed, and you want to distribute only entities that changed
during a span of time
```csharp

// create new instance of a collector
var collector = new ComponentChangesCollector(ecsRepo, new[] { typeof(ICar) }); 

// update and delete some entities in your repository
IEntity e1 = repo.CreateWithComponents(new Sedan("a")); 
IEntity e2 = repo.CreateWithComponents(new Sedan("b"));
IEntity e3 = repo.CreateWithComponents(new Suv("c"));
repo.Delete(e2);
repo.Delete(e3);

//using the Pop() function will create a report and reset the collector
CollectorReport collectorReport = collector.Pop();

// the collector report holds all the entities changed or deleted during the time span.
EntityUpdatedEventArgs[] updated = collectorReport.Updated;
EntityDeletedEventArgs[] deleted = collectorReport.Deleted;



```

# Distribution of entities and components
the ECS in itself does not supply a distribution mechanism.  It does supply utilities to assist in distributing data to other nodes using standard messageing transports.
- EcsPackage - A container that is used to aggregate ECS changes
- EcsPackage converters - A System.Text.Json converter, and Newtonsoft.Json converter for serializing/deserilizing the Ecs EcsPackage
- Merge function in the Ecs repository, that can import the EcsPackage and invoke the changes contained inside

## EcsPackage
### updating manually :
```csharp
EcsPackage ecsPackage = new EcsPackage();

IEntity entity1 = repo1.Create(sedan1, suv1, location);
ecsPackage.AddAllComponents(entity1)  //will add all components of entity1 to the distribution pack, resulting the entity to be created/updated in the receiving node
          .AddComponent<Sedan>(entity2) // will add sedan Component of the entity to the distribution package, resulting the entity to be created/updated in the receiving node
          .AddDeletedEntity(entity3) // will add a 'deleted' entity to the package, resulting in the entity being deleted in the receiving node
          .AddDeleteByTag("foo") // will add a tag for deletion, resulting in all the entities tagged with "foo" to be deleted from the receiving node                    

```

### updating from event args :
```csharp
EcsPackage ecsPackage = new EcsPackage();

void onEventsGlobalUpdated(EntitiesUpdatedEventArgs args) => ecsPackage.AddFromEvent(args); // event handlers
void onEventsGlobalDeleted(EntitiesDeletedEventArgs args) => ecsPackage.AddFromEvent(args); // event handlers

repo1.Events.GlobalUpdated += onEventsGlobalUpdated;  // register to event
repo1.Events.GlobalDeleted += onEventsGlobalDeleted;  // register to event

// all updated and deleted entities will be added to the ecsPackage

```

### merging EcsPackage on a receiving node
upon receiving and deserializing the ecs package from your transport, use the merging function in the target repository
``` csharp
destinationRepository.MergePackage(ecsPackage);
```

