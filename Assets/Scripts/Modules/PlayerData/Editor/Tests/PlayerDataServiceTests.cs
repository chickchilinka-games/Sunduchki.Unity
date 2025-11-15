using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Modules.PlayerData.Data;
using Modules.PlayerData.Interfaces;
using Modules.PlayerData.Services;
using NUnit.Framework;
using Zenject;

namespace Modules.PlayerData.Editor.Tests
{
    public class PlayerDataServiceTests : ZenjectUnitTestFixture
    {
        private static PlayerModuleData M(string n, int r, string p, int b = 0, bool pend = false)
            => new() { ModuleName = n, RemoteVersion = r, Payload = p, BaseVersion = b, IsPendingUpload = pend };

        [SetUp]
        public void SetupSerializer()
        {
            Container.Bind<ISerializer>().To<JsonSerializer>().AsSingle();
        }

        [Test]
        public async Task Save_RemoteAvailable_PayloadChanged()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 1 },
                RemoteData =
                {
                    ["A"] = M("A", 1, "{\"old\":1}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 1, "{\"old\":1}")
                }
            };

            var cA = new TestConsumer("A") { NextSerialized = "{\"new\":2}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            Assert.AreEqual(2, cache.Local["A"].RemoteVersion);
            Assert.AreEqual("{\"new\":2}", provider.RemoteData["A"].Payload);
        }

        [Test]
        public async Task Save_RemoteAvailable_NoChanges()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 5 },
                RemoteData =
                {
                    ["A"] = M("A", 5, "{\"same\":5}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 5, "{\"same\":5}")
                }
            };

            var cA = new TestConsumer("A") { NextSerialized = "{\"same\":5}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            Assert.AreEqual(0, provider.Saved.Count);
        }

        [Test]
        public async Task Save_RemoteUnavailable_PayloadChanged()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 2, "{\"old\":2}")
                }
            };

            var cA = new TestConsumer("A") { NextSerialized = "{\"offline\":3}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            Assert.IsTrue(cache.Local["A"].IsPendingUpload);
            Assert.AreEqual(3, cache.Local["A"].RemoteVersion);
        }

        [Test]
        public async Task Save_RemoteUnavailable_NoChanges()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 4, "{\"same\":4}")
                }
            };

            var cA = new TestConsumer("A") { NextSerialized = "{\"same\":4}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            Assert.AreEqual(0, cache.Saved.Count);
        }

        [Test]
        public async Task Load_RemoteAvailable_RemoteNewer()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 10 },
                RemoteData =
                {
                    ["A"] = M("A", 10, "{\"srv\":10}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 9, "{\"local\":9}")
                }
            };

            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);

            Assert.AreEqual("{\"srv\":10}", cA.AppliedPayload);
            Assert.AreEqual(10, cache.Local["A"].RemoteVersion);
        }

        [Test]
        public async Task Load_RemoteAvailable_VersionsEqual()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 5 },
                RemoteData =
                {
                    ["A"] = M("A", 5, "{\"srv\":5}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 5, "{\"local\":5}")
                }
            };

            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);

            Assert.AreEqual("{\"local\":5}", cA.AppliedPayload);
            Assert.AreEqual(0, provider.LoadCalls.Count);
        }

        [Test]
        public async Task Load_RemoteAvailable_NoCache()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 1 },
                RemoteData =
                {
                    ["A"] = M("A", 1, "{\"srv\":1}")
                }
            };

            var cache = new FakeCache();
            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);

            Assert.AreEqual("{\"srv\":1}", cA.AppliedPayload);
            Assert.AreEqual(1, cache.Local["A"].RemoteVersion);
        }

        [Test]
        public async Task Load_RemoteAvailable_PendingUploadWithMatchingBase()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 3 },
                RemoteData =
                {
                    ["A"] = M("A", 3, "{\"srv\":3}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 3, "{\"local\":3}", b: 3, pend: true)
                }
            };

            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);

            Assert.IsFalse(cache.Local["A"].IsPendingUpload);
            Assert.AreEqual(cache.Local["A"].Payload, cA.AppliedPayload);
        }

        [Test]
        public async Task Load_RemoteUnavailable_CacheComplete()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 7, "{\"local\":7}")
                }
            };

            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);
            Assert.AreEqual("{\"local\":7}", cA.AppliedPayload);
        }

        [Test]
        public async Task Load_RemoteUnavailable_CacheIncomplete()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache();

            var cA = new TestConsumer("A");

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, err) = await svc.TryLoad();
            Assert.IsFalse(ok);
            StringAssert.Contains("cache incomplete", err);
        }
        
        [Test]
        public async Task Save_RemoteAvailable_NewModule_NoCache()
        {
            var provider = new FakeProvider();

            var cache = new FakeCache(); // no "A" locally
            var cA = new TestConsumer("A") { NextSerialized = "{\"first\":1}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            var saved = provider.Saved[0][0];
            Assert.AreEqual("A", saved.ModuleName);
            Assert.AreEqual("{\"first\":1}", saved.Payload);
            Assert.AreEqual(0, saved.BaseVersion, "BaseVersion should be 0 when remote has no key");
            Assert.AreEqual(1, saved.RemoteVersion, "RemoteVersion increments from 0 to 1 on first save");

            Assert.AreEqual(0, cache.Local["A"].BaseVersion);
            Assert.AreEqual(1, cache.Local["A"].RemoteVersion);
            Assert.IsFalse(cache.Local["A"].IsPendingUpload);
        }
        
        [Test]
        public async Task Save_RemoteUnavailable_NewModule_NoCache()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache(); // no "A" locally
            var cA = new TestConsumer("A") { NextSerialized = "{\"first_offline\":1}" };

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            var a = cache.Local["A"];
            Assert.AreEqual("{\"first_offline\":1}", a.Payload);
            Assert.AreEqual(0, a.BaseVersion, "BaseVersion should start at 0 offline");
            Assert.AreEqual(1, a.RemoteVersion, "Local RemoteVersion should bump to 1 offline");
            Assert.IsTrue(a.IsPendingUpload);
            Assert.AreEqual(0, provider.Saved.Count, "No remote calls when offline");
        }
        
        [Test]
        public async Task Load_RemoteAvailable_RemoteMissingKey_UseCache()
        {
            var provider = new FakeProvider();

            var cache = new FakeCache
            {
                Local = { ["A"] = M("A", 2, "{\"local\":2}") }
            };

            var cA = new TestConsumer("A");
            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);
            
            Assert.AreEqual("{\"local\":2}", cA.AppliedPayload);
        }

        [Test]
        public async Task Load_RemoteAvailable_PendingUploadWithStaleBase_ServerWins_LoadsRemoteAndClearsPending()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 5 },
                RemoteData = { ["A"] = M("A", 5, "{\"srv\":5}", b: 5) }
            };

            var cache = new FakeCache
            {
                Local = { ["A"] = new PlayerModuleData { ModuleName = "A", Payload = "{\"local_pending\":4}", RemoteVersion = 4, BaseVersion = 3, IsPendingUpload = true } }
            };

            var cA = new TestConsumer("A");
            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);

            Assert.AreEqual("{\"srv\":5}", cA.AppliedPayload);
            Assert.AreEqual(5, cache.Local["A"].RemoteVersion);
            Assert.AreEqual(5, cache.Local["A"].BaseVersion);
            Assert.IsFalse(cache.Local["A"].IsPendingUpload);
        }
        
        [Test]
        public async Task Save_RemoteAvailable_PartialChanges_Mixed()
        {
            var provider = new FakeProvider
            {
                RemoteVersions = { ["A"] = 2, ["B"] = 7 },
                RemoteData =
                {
                    ["A"] = M("A", 2, "{\"a\":2}"),
                    ["B"] = M("B", 7, "{\"b\":7}")
                }
            };

            var cache = new FakeCache
            {
                Local =
                {
                    ["A"] = M("A", 2, "{\"a\":2}"),
                    ["B"] = M("B", 7, "{\"b\":7}")
                }
            };

            var cA = new TestConsumer("A") { NextSerialized = "{\"a\":3}" }; // changed
            var cB = new TestConsumer("B") { NextSerialized = "{\"b\":7}" }; // same

            Bind(provider, cache, cA, cB);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            Assert.AreEqual(1, provider.Saved.Count, "Only changed module should be saved");
            var saved = provider.Saved[0][0];
            Assert.AreEqual("A", saved.ModuleName);
            Assert.AreEqual(2, saved.BaseVersion);
            Assert.AreEqual(3, saved.RemoteVersion);
            Assert.AreEqual("{\"a\":3}", saved.Payload);
        }
        
        [Test]
        public async Task Save_RemoteUnavailable_NoChanges_ButPending_ShouldNotBump()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache
            {
                Local = { ["A"] = M("A", 3, "{\"local\":3}", b: 3, pend: true) }
            };
            var cA = new TestConsumer("A") { NextSerialized = "{\"local\":3}" }; // unchanged

            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TrySave();
            Assert.IsTrue(ok);

            var a = cache.Local["A"];
            Assert.AreEqual(3, a.RemoteVersion, "Version must not bump again");
            Assert.IsTrue(a.IsPendingUpload, "Still pending");
            Assert.AreEqual(0, cache.Saved.Count, "No writes to cache expected on no-op (optional if your FakeCache tracks no-op saves)");
        }
        
        [Test]
        public async Task Load_RemoteUnavailable_CacheWithPending_ApplyCache()
        {
            var provider = new FakeProvider { Available = false };
            var cache = new FakeCache
            {
                Local = { ["A"] = M("A", 4, "{\"local_pending\":4}", b: 4, pend: true) }
            };

            var cA = new TestConsumer("A");
            Bind(provider, cache, cA);
            var svc = Container.Resolve<PlayerDataService>();

            var (ok, _) = await svc.TryLoad();
            Assert.IsTrue(ok);
            Assert.AreEqual("{\"local_pending\":4}", cA.AppliedPayload);
        }
        
        private void Bind(FakeProvider provider, FakeCache cache, params IPlayerDataConsumer[] consumers)
        {
            Container.Bind<IPlayerDataProvider>().FromInstance(provider);
            Container.Bind<IPlayerDataCache>().FromInstance(cache);
            Container.Bind<IPlayerDataConsumersCollector>().FromInstance(new TestCollector(consumers));
            Container.Bind<PlayerDataService>().AsSingle();
        }
    }
}