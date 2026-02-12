using System.ComponentModel;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AppData.Data;
using Modules.AppData.Interfaces;
using Modules.AppData.Services;
using NUnit.Framework;
using Zenject;

namespace Modules.AppData.Editor.Tests
{
    public class AppDataServiceTests : ZenjectUnitTestFixture
    {
        private static AppModuleData CreateData(string name, int version, string json) =>
            new() { ModuleName = name, Version = version, Payload = json };

        [SetUp]
        public void CommonInstall()
        {
            Container.Bind<ISerializer>().To<JsonSerializer>().AsSingle();
        }

        [Test]
        public async Task ProviderAvailable_RemoteNewer_UpdatesCache_And_Applies()
        {
            var provider = new FakeProvider();
            provider.RemoteVersions["A"] = 2;
            provider.RemoteData["A"] = CreateData("A", 2, "{\"remote\":2}");

            var cache = new FakeCache();
            var cA = new TestConsumer("A");

            Container.Bind<IAppDataProvider>().FromInstance(provider);
            Container.Bind<IAppDataCache>().FromInstance(cache);
            Container.Bind<IAppDataConsumersCollector>().FromInstance(new TestCollector(cA));
            Container.Bind<AppDataService>().AsSingle();

            var service = Container.Resolve<AppDataService>();
            var (ok, err) = await service.TryLoad();

            Assert.IsTrue(ok, err);
            Assert.AreEqual(2, cache.LocalData["A"].Version);
            Assert.AreEqual("{\"remote\":2}", cA.LastSerializedPayload);
        }

        [Test]
        public async Task ProviderUnavailable_CacheComplete_LoadsFromCache()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache();
            cache.LocalData["B"] = CreateData("B", 5, "{\"local\":5}");
            var cB = new TestConsumer("B");

            Container.Bind<IAppDataProvider>().FromInstance(provider);
            Container.Bind<IAppDataCache>().FromInstance(cache);
            Container.Bind<IAppDataConsumersCollector>().FromInstance(new TestCollector(cB));
            Container.Bind<AppDataService>().AsSingle();

            var service = Container.Resolve<AppDataService>();
            var (ok, err) = await service.TryLoad();

            Assert.IsTrue(ok, err);
            Assert.AreEqual("{\"local\":5}", cB.LastSerializedPayload);
        }

        [Test]
        public async Task ProviderUnavailable_CacheIncomplete_ReturnsError()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache();
            var cC = new TestConsumer("C");

            Container.Bind<IAppDataProvider>().FromInstance(provider);
            Container.Bind<IAppDataCache>().FromInstance(cache);
            Container.Bind<IAppDataConsumersCollector>().FromInstance(new TestCollector(cC));
            Container.Bind<AppDataService>().AsSingle();

            var service = Container.Resolve<AppDataService>();
            var (ok, err) = await service.TryLoad();

            Assert.IsFalse(ok);
            StringAssert.Contains("Version cache is incomplete", err);
        }
    }
}
