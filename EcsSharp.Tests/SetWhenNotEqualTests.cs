using NUnit.Framework;

namespace EcsSharp.Tests;

internal class SetWhenNotEqualTests
{
    private readonly IEcsRepoFactory m_factory = new DefaultEcsRepoFactory();

    [Test]
    public void SimpleWhenNotExists()
    {
        IEcsRepo repo = getNewRepo();

        Sedan sedan1 = new("A");
        IEntity entity1 = repo.Create();
        bool res = entity1.SetWhenNotEqual(sedan1);
        Assert.AreEqual(sedan1, entity1.GetComponent<Sedan>());
        Assert.IsTrue(res);
    }

    [Test]
    public void Simple_ComponentEquals()
    {
        IEcsRepo repo = getNewRepo();

        int updateCounter = 0;
        repo.Events.ComponentUpdated[typeof(Location)] += (_) => updateCounter++;
        Location location1 = new(1,1,1);
        Location location2 = new(1,1,1);
        IEntity entity1 = repo.CreateWithComponents(location1);
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500,100));
        bool res = entity1.SetWhenNotEqual(location2);
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500));
        Assert.IsFalse(res);
    }

    [Test]
    public void Simple_ComponentNotEquals()
    {
        IEcsRepo repo = getNewRepo();

        int updateCounter = 0;
        repo.Events.ComponentUpdated[typeof(Location)] += (_) => updateCounter++;
        Location location1 = new(1,1,1);
        Location location2 = new(2,2,2);
        IEntity entity1 = repo.CreateWithComponents(location1);
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500,100));
        bool res = entity1.SetWhenNotEqual(location2);
        Assert.That(()=>updateCounter, Is.EqualTo(2).After(500,100));
        Assert.IsTrue(res);
    }

    [Test]
    public void UpdateWithFactory_enableOnNotExists()
    {
        IEcsRepo repo = getNewRepo();

        Sedan sedan1 = new("A");
        IEntity entity1 = repo.Create();
        bool res = entity1.SetWhenNotEqual<Sedan>(_=>sedan1);
        Assert.AreEqual(sedan1, entity1.GetComponent<Sedan>());
        Assert.IsTrue(res);
    }

    [Test]
    public void UpdateWithFactory_disableOnNotExists()
    {
        IEcsRepo repo = getNewRepo();
        Sedan sedan1 = new("A");
        IEntity entity1 = repo.Create();
        bool res = entity1.SetWhenNotEqual<Sedan>(_ => sedan1, false);
        Assert.IsNull(entity1.GetComponent<Sedan>());
        Assert.IsFalse(res);
    }



    [Test]
    public void UpdateWithFactory_ComponentExists()
    {
        IEcsRepo repo = getNewRepo();

        Sedan sedan2 = new("B");
        IEntity entity1 = repo.CreateWithComponents(sedan2);
        bool res = entity1.SetWhenNotEqual<Sedan>(current => new Sedan(((char)(current.Id[0] + 1)).ToString()));
        IEntity q = repo.QuerySingle<Sedan>();
        Assert.AreEqual("C", q.GetComponent<Sedan>().Id);
        Assert.IsTrue(res);
    }

    
    [Test]
    public void UpdateWithFactory_ComponentEquals()
    {
        IEcsRepo repo = getNewRepo();

        int updateCounter = 0;
        repo.Events.ComponentUpdated[typeof(Location)] += (_) => updateCounter++;
        Location location1 = new(1,1,1);
        Location location2 = new(1,1,1);
        IEntity entity1 = repo.CreateWithComponents(location1);
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500,100));
        bool res = entity1.SetWhenNotEqual<Location>((current)=>new Location(current.X,current.Y,current.Z));
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500));
        Assert.IsFalse(res);
    }

    [Test]
    public void UpdateWithFactory_ComponentNotEquals()
    {
        IEcsRepo repo = getNewRepo();

        int updateCounter = 0;
        repo.Events.ComponentUpdated[typeof(Location)] += (_) => updateCounter++;
        Location location1 = new(1,1,1);
        Location location2 = new(1,1,1);
        IEntity entity1 = repo.CreateWithComponents(location1);
        Assert.That(()=>updateCounter, Is.EqualTo(1).After(500,100));
        bool res = entity1.SetWhenNotEqual<Location>((current)=>new Location(current.X +1,current.Y +1,current.Z +1));
        Assert.That(()=>updateCounter, Is.EqualTo(2).After(500));
        Assert.IsTrue(res);
    }

    private IEcsRepo getNewRepo()
    {
        IEcsRepo ecsRepo = m_factory.Create();
        return ecsRepo;
    }
}