using System.Collections.Generic;
using System.Linq;

using EcsSharp.Events;
using EcsSharp.Helpers;
using EcsSharp.Storage;

using log4net.Config;

using NUnit.Framework;

namespace EcsSharp.Tests
{
    public class CustomTypeFamilyProviderTests
    {
        public CustomTypeFamilyProviderTests()
        {
            BasicConfigurator.Configure();
        }

        [Test]
        public void Query_WithFamily()
        {
            ExplicitInterfacesFamilyProvider p = new ExplicitInterfacesFamilyProvider();
            p.Add(typeof(Sedan), typeof(ICar));
            p.Add(typeof(Suv),   typeof(ICar));

            IEcsRepo repo = getNewRepoWithInterfaceMapping(p);

            repo.EntityBuilder().WithComponents(new Sedan()).Build();
            repo.EntityBuilder().WithComponents(new Suv()).Build();

            Assert.AreEqual(2, repo.Query<ICar>().ToHashSet().Count);
            Assert.AreEqual(2, repo.QueryAll().Count);
        }

        [Test]
        public void Query_WithoutFamily()
        {
            ExplicitInterfacesFamilyProvider p = new ExplicitInterfacesFamilyProvider();

            IEcsRepo repo = getNewRepoWithInterfaceMapping(p);

            repo.EntityBuilder().WithComponents(new Sedan()).Build();
            repo.EntityBuilder().WithComponents(new Suv()).Build();

            HashSet<IEntity> hashSet = repo.Query<ICar>().ToHashSet();
            Assert.AreEqual(0, hashSet.Count);

            Assert.AreEqual(2, repo.QueryAll().Count);
            Assert.AreEqual(1, repo.Query<Sedan>().ToHashSet().Count);
        }

        private IEcsRepo getNewRepoWithInterfaceMapping(ITypeFamilyProvider tfp)
        {


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
        }
    }
}