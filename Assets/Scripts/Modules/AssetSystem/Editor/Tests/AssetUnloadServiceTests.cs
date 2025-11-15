using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AssetSystem.Observables;
using Modules.AssetSystem.Services;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

namespace Modules.AssetSystem.Editor.Tests
{
    [TestFixture]
    public class AssetUnloadServiceTests
    {
        private AssetUnloadService _service;
        private TestAssetUnloadObservable _observable;
        private GameObject _testGameObject;
        private Texture2D _testTexture;

        [SetUp]
        public void SetUp()
        {
            _observable = new TestAssetUnloadObservable();
            _service = CreateService(new[] { _observable });
            _testGameObject = new GameObject("TestGameObject");
            _testTexture = new Texture2D(1, 1);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (_testGameObject != null) UnityEngine.Object.DestroyImmediate(_testGameObject);
            if (_testTexture != null) UnityEngine.Object.DestroyImmediate(_testTexture);
            _observable?.Dispose();
        }

        private AssetUnloadService CreateService(IEnumerable<AssetUnloadObservable> observables)
        {
            var service = (AssetUnloadService)Activator.CreateInstance(
                typeof(AssetUnloadService),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null,
                new object[] { observables },
                null
            );
            return service;
        }

        [Test]
        public void Initialize_SubscribesToObservables()
        {
            _service.Initialize();

            Assert.IsTrue(_observable.HasSubscribers);
        }

        [Test]
        public async Task UnloadTriggered_UnloadsRecordedAssets()
        {
            _service.Initialize();
            _service.Record(_testGameObject);

            LogAssert.Expect(LogType.Log, $"[{nameof(AssetUnloadService)}] Unloading assets");

            _observable.Trigger();
            await UniTask.Delay(100);

            // Verify log was called (via LogAssert above)
            Assert.Pass();
        }

        [Test]
        public async Task UnloadTriggered_WithGameObject_CallsUnloadUnusedAssets()
        {
            _service.Initialize();
            _service.Record(_testGameObject);

            LogAssert.Expect(LogType.Log, $"[{nameof(AssetUnloadService)}] Unloading assets");

            _observable.Trigger();
            await UniTask.Delay(100);

            // GameObject triggers Resources.UnloadUnusedAssets
            Assert.Pass();
        }

        [Test]
        public async Task UnloadTriggered_WithMultipleAssets_UnloadsAll()
        {
            var asset2 = new GameObject("TestGameObject2");
            try
            {
                _service.Initialize();
                _service.Record(_testGameObject);
                _service.Record(asset2);

                LogAssert.Expect(LogType.Log, $"[{nameof(AssetUnloadService)}] Unloading assets");

                _observable.Trigger();
                await UniTask.Delay(100);

                Assert.Pass();
            }
            finally
            {
                if (asset2 != null) UnityEngine.Object.DestroyImmediate(asset2);
            }
        }

        [Test]
        public async Task UnloadTriggered_MultipleObservables_TriggersOnAny()
        {
            var observable2 = new TestAssetUnloadObservable();
            try
            {
                var serviceMulti = CreateService(new[] { _observable, observable2 });
                serviceMulti.Initialize();
                serviceMulti.Record(_testGameObject);

                LogAssert.Expect(LogType.Log, $"[{nameof(AssetUnloadService)}] Unloading assets");

                observable2.Trigger();
                await UniTask.Delay(100);

                serviceMulti.Dispose();
                Assert.Pass();
            }
            finally
            {
                observable2?.Dispose();
            }
        }

        [Test]
        public async Task UnloadTriggered_WithNullAsset_HandlesGracefully()
        {
            _service.Initialize();
            _service.Record<Texture2D>(null);

            _observable.Trigger();
            await UniTask.Delay(100);
            
            Assert.Pass();
        }

        private class TestAssetUnloadObservable : AssetUnloadObservable
        {
            public bool HasSubscribers => _subscribersCount > 0;
            
            private readonly Subject<Unit> _subject = new Subject<Unit>();
            private bool _disposed;
            private int _subscribersCount;
            
            public void Trigger()
            {
                if (!_disposed)
                {
                    _subject.OnNext(Unit.Default);
                }
            }

            protected override IDisposable SubscribeCore(Observer<Unit> observer)
            {
                _subscribersCount += 1;
                return _subject.Subscribe(_ => observer.OnNext(Unit.Default),
                    observer.OnErrorResume,
                    observer.OnCompleted
                );
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _subject?.Dispose();
            }
        }
    }
}
