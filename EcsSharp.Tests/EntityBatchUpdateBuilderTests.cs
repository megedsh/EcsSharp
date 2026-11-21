using NUnit.Framework;

namespace EcsSharp.Tests;

[TestFixture]
internal class EntityBatchUpdateBuilderTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    [Test]
    public void Run_AppliesSetComponentAndSetComponents()
    {
        IEcsRepo repo = getNewRepo();
        IEntity entity = repo.Create();
        Sedan sedan = new("sedan");
        Suv suv = new("suv");
        Location location = new(1, 2, 3);

        var builder = entity.BatchUpdateBuilder()
                            .WithSetComponent(sedan)
                            .WithSetComponents(suv, location);

        Assert.Multiple(() =>
        {
            Assert.IsNull(entity.GetComponent<Sedan>());
            Assert.IsNull(entity.GetComponent<Suv>());
            Assert.IsNull(entity.GetComponent<Location>());
        });

        IEntity result = builder.Run();

        Assert.Multiple(() =>
        {
            Assert.AreSame(entity, result);
            Assert.AreSame(sedan, entity.GetComponent<Sedan>());
            Assert.AreSame(suv, entity.GetComponent<Suv>());
            Assert.AreSame(location, entity.GetComponent<Location>());
        });
    }

    [Test]
    public void Run_AppliesSetWithVersion()
    {
        IEcsRepo repo = getNewRepo();
        IEntity entity = repo.Create();
        Sedan sedan = new("versioned");

        entity.BatchUpdateBuilder()
              .WithSetWithVersion(sedan, 42)
              .Run();

        Component component = entity.GetComponent(typeof(Sedan));
        Assert.Multiple(() =>
        {
            Assert.AreSame(sedan, component.Data);
            Assert.AreEqual(42, component.Version);
        });
    }

    [Test]
    public void Run_AppliesBothConditionalSetOverloads()
    {
        IEcsRepo repo = getNewRepo();
        IEntity entity = repo.CreateWithComponents(new Sedan("A"), new Suv("A"));

        entity.BatchUpdateBuilder()
              .WithConditionalSet(new Sedan("B"), (current, _) => current.Id == "A")
              .WithConditionalSet<Suv>(current => current.Id == "A", _ => new Suv("B"))
              .Run();

        Assert.Multiple(() =>
        {
            Assert.AreEqual("B", entity.GetComponent<Sedan>().Id);
            Assert.AreEqual("B", entity.GetComponent<Suv>().Id);
        });
    }

    [Test]
    public void Run_AppliesSetWhenNotEqualAndUpdateComponentOverloads()
    {
        IEcsRepo repo = getNewRepo();
        IEntity entity = repo.CreateWithComponents(new Sedan("A"), new Suv("A"), new Location(1, 1, 1));

        entity.BatchUpdateBuilder()
              .WithSetWhenNotEqual(new Location(2, 2, 2))
              .WithSetWhenNotEqual<Sedan>(current => new Sedan(current.Id + "B"))
              .WithUpdateComponent<Suv>(current => new Suv(current.Id + "B"))
              .Run();

        Assert.Multiple(() =>
        {
            Assert.AreEqual(new Location(2, 2, 2), entity.GetComponent<Location>());
            Assert.AreEqual("AB", entity.GetComponent<Sedan>().Id);
            Assert.AreEqual("AB", entity.GetComponent<Suv>().Id);
        });
    }

    [Test]
    public void Run_RespectsSetWhenNotExistFalse()
    {
        IEcsRepo repo = getNewRepo();
        IEntity entity = repo.Create();

        entity.BatchUpdateBuilder()
              .WithConditionalSet<Sedan>(_ => true, _ => new Sedan("conditional"), false)
              .WithSetWhenNotEqual<Suv>(_ => new Suv("not-equal"), false)
              .WithUpdateComponent<Location>(_ => new Location(1, 2, 3), false)
              .Run();

        Assert.Multiple(() =>
        {
            Assert.IsFalse(entity.HasComponent<Sedan>());
            Assert.IsFalse(entity.HasComponent<Suv>());
            Assert.IsFalse(entity.HasComponent<Location>());
        });
    }

    [Test]
    public void BatchUpdateBuilder_WhenReusedFromPool_TargetsRequestingEntity()
    {
        IEcsRepo repo = getNewRepo();
        IEntity first = repo.Create();
        IEntity second = repo.Create();

        first.BatchUpdateBuilder().WithSetComponent(new Sedan("first")).Run();
        second.BatchUpdateBuilder().WithSetComponent(new Sedan("second")).Run();

        Assert.Multiple(() =>
        {
            Assert.AreEqual("first", first.GetComponent<Sedan>().Id);
            Assert.AreEqual("second", second.GetComponent<Sedan>().Id);
        });
    }

    private IEcsRepo getNewRepo() => m_factory.Create();
}
