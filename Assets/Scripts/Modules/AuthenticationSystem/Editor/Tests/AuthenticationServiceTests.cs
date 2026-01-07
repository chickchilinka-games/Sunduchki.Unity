using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Entities;
using Modules.AuthenticationSystem.Interfaces;
using Modules.AuthenticationSystem.Services;
using Modules.AuthenticationSystem.Utils;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Modules.AuthenticationSystem.Editor.Tests
{
    [TestFixture]
    public sealed class AuthenticationServiceTests
    {
        [Test]
        public async Task TryAutoSignIn_ReturnsFailure_WhenNoCachedContext()
        {
            var model = new AuthenticationModel();
            var authority = new FakeIdentityAuthority();
            var service = new AuthenticationService(Array.Empty<ICredentialProvider>(), authority, model);

            var result = await service.TryAutoSignInAsync();

            Assert.IsFalse(result.Success);
            Assert.IsNull(service.UserContextStream.CurrentValue);
            Assert.AreEqual(AuthStatus.None, service.AuthStatusStream.CurrentValue);
        }

        [Test]
        public async Task TryAutoSignIn_UsesCachedContext_WhenAvailable()
        {
            var cachedContext = CreateUserContext("cached-user", AuthType.Google);

            var model = new AuthenticationModel();
            var authority = new FakeIdentityAuthority
            {
                CachedUserFactory = () => UniTask.FromResult(cachedContext)
            };

            var service = new AuthenticationService(Array.Empty<ICredentialProvider>(), authority, model);

            var result = await service.TryAutoSignInAsync();

            Assert.IsTrue(result.Success);
            Assert.AreSame(cachedContext, service.UserContextStream.CurrentValue);
            Assert.AreEqual(AuthStatus.SignedIn, service.AuthStatusStream.CurrentValue);
        }

        [Test]
        public async Task SignIn_DelegatesToProviderAndAuthority()
        {
            var credential = AuthCredentialBuilder.For(AuthType.Google).WithUsername("user").Build();
            var returnedContext = CreateUserContext("signed-user", AuthType.Google);

            var provider = new CapturingCredentialProvider(AuthType.Google, credential);
            var model = new AuthenticationModel();
            var authority = new FakeIdentityAuthority
            {
                SignInHandler = incoming =>
                {
                    Assert.AreSame(credential, incoming);
                    return UniTask.FromResult(returnedContext);
                }
            };

            var service = new AuthenticationService(new ICredentialProvider[] { provider }, authority, model);

            var result = await service.SignInAsync(AuthType.Google);

            Assert.IsTrue(result.Success);
            Assert.AreSame(returnedContext, result.UserContext);
            Assert.AreEqual(1, provider.CallCount);
            Assert.AreEqual(AuthStatus.SignedIn, service.AuthStatusStream.CurrentValue);
            Assert.AreSame(returnedContext, service.UserContextStream.CurrentValue);
        }

        [Test]
        public async Task SignIn_WhenAuthorityFails_FailsSignInAndResetsStatus()
        {
            LogAssert.Expect(LogType.Error, "[Authentication] Sign-in failed for Google: boom");

            var provider = new CapturingCredentialProvider(AuthType.Google,
                AuthCredentialBuilder.For(AuthType.Google).Build());

            var model = new AuthenticationModel();
            var authority = new FakeIdentityAuthority
            {
                SignInHandler = _ => throw new InvalidOperationException("boom")
            };

            var service = new AuthenticationService(new ICredentialProvider[] { provider }, authority, model);

            var result = await service.SignInAsync(AuthType.Google);

            Assert.IsFalse(result.Success);
            Assert.AreEqual(AuthStatus.SignedOut, service.AuthStatusStream.CurrentValue);
            Assert.IsNull(service.UserContextStream.CurrentValue);
        }

        [Test]
        public async Task Unlink_RefreshesContext_WhenAuthorityProvidesUpdatedUser()
        {
            var initialContext = CreateUserContext("user", AuthType.Google, AuthType.Apple);
            var updatedContext = CreateUserContext("user", AuthType.Apple);

            var model = new AuthenticationModel();
            model.CompleteSignIn(initialContext);

            var authority = new FakeIdentityAuthority
            {
                UnlinkHandler = _ => UniTask.CompletedTask,
                CachedUserFactory = () => UniTask.FromResult(updatedContext)
            };

            var service = new AuthenticationService(Array.Empty<ICredentialProvider>(), authority, model);
            await service.UnlinkAsync(AuthType.Google);

            Assert.AreSame(updatedContext, service.UserContextStream.CurrentValue);
            Assert.IsFalse(service.UserContextStream.CurrentValue.LinkedProviders.Any(l => l.AuthType == AuthType.Google));
        }

        [Test]
        public async Task Unlink_RemovesLinkLocally_WhenAuthorityReturnsNullContext()
        {
            var initialContext = CreateUserContext("user", AuthType.Google, AuthType.Apple);

            var model = new AuthenticationModel();
            model.CompleteSignIn(initialContext);

            var authority = new FakeIdentityAuthority
            {
                UnlinkHandler = _ => UniTask.CompletedTask,
                CachedUserFactory = () => UniTask.FromResult<UserContext>(null)
            };

            var service = new AuthenticationService(Array.Empty<ICredentialProvider>(), authority, model);
            await service.UnlinkAsync(AuthType.Google);

            var linked = service.UserContextStream.CurrentValue.LinkedProviders
                .Select(l => l.AuthType)
                .ToArray();

            CollectionAssert.DoesNotContain(linked, AuthType.Google);
        }

        private static UserContext CreateUserContext(string userId, params AuthType[] linkedTypes)
        {
            var linkageInfos = linkedTypes?
                .Select(t => new LinkageInfo(t, $"{t}User", $"{t}@mail.test"))
                .ToList() ?? new List<LinkageInfo>();

            return new UserContext(userId, $"{userId}-name", "user", "token", linkageInfos);
        }

        private sealed class CapturingCredentialProvider : ICredentialProvider
        {
            private readonly AuthCredential _credential;

            public CapturingCredentialProvider(AuthType authType, AuthCredential credential)
            {
                AuthType = authType;
                _credential = credential;
            }

            public AuthType AuthType { get; }

            public int CallCount { get; private set; }

            public UniTask<AuthCredential> AcquireCredentialAsync()
            {
                CallCount++;
                return UniTask.FromResult(_credential);
            }
        }

        private sealed class FakeIdentityAuthority : IIdentityAuthority
        {
            public Func<UniTask<UserContext>> CachedUserFactory { get; set; } =
                () => UniTask.FromResult<UserContext>(null);

            public Func<AuthCredential, UniTask<UserContext>> SignInHandler { get; set; } =
                _ => UniTask.FromResult<UserContext>(null);

            public Func<AuthCredential, UniTask<LinkageInfo>> LinkHandler { get; set; } =
                _ => UniTask.FromResult<LinkageInfo>(null);

            public Func<AuthType, UniTask> UnlinkHandler { get; set; } = _ => UniTask.CompletedTask;

            public Func<AuthType, UniTask<bool>> IsLinkedHandler { get; set; } =
                _ => UniTask.FromResult(false);

            public Func<UniTask> SignOutHandler { get; set; } = () => UniTask.CompletedTask;
            public Func<CancellationToken, UniTask<UserContext>> RefreshHandler { get; set; } =
                _ => UniTask.FromResult<UserContext>(null);

            public UniTask<UserContext> GetCachedUserContextAsync() => CachedUserFactory();

            public UniTask<UserContext> SignInAsync(AuthCredential credential) => SignInHandler(credential);

            public UniTask<LinkageInfo> LinkAsync(AuthCredential credential) => LinkHandler(credential);
            public UniTask DeleteAsync(AuthCredential credential)
            {
                return UniTask.CompletedTask;
            }

            public UniTask UnlinkAsync(AuthType authType) => UnlinkHandler(authType);

            public UniTask SignOutAsync() => SignOutHandler();

            public UniTask<UserContext> RefreshAsync(CancellationToken cancellationToken = default)
                => RefreshHandler(cancellationToken);
        }
    }
}
