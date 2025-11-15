using System;
using System.Collections.Generic;
using Modules.AuthenticationSystem.Data;
using Modules.AuthenticationSystem.Utils;
using NUnit.Framework;

namespace Modules.AuthenticationSystem.Editor.Tests
{
    [TestFixture]
    public sealed class AuthCredentialBuilderTests
    {
        [Test]
        public void ParseGoogle_ReturnsExpectedTokens()
        {
            var credential = AuthCredentialBuilder.For(AuthType.Google)
                .WithUsername("user@gmail.com")
                .WithDisplayName("Google User")
                .WithIdToken("id-token")
                .WithAccessToken("access-token")
                .WithScopes(new[] { "email", "profile" })
                .WithCustomParameters(new Dictionary<string, string> { { "prompt", "select_account" } })
                .Build();

            var result = AuthCredentialBuilder.ParseGoogle(credential);

            Assert.AreEqual(AuthType.Google, result.AuthType);
            Assert.AreEqual("id-token", result.IdToken);
            Assert.AreEqual("access-token", result.AccessToken);
            CollectionAssert.AreEquivalent(new[] { "email", "profile" }, result.Scopes);
            Assert.AreEqual("Google User", result.DisplayName);
            Assert.AreEqual("select_account", result.CustomParameters["prompt"]);
        }

        [Test]
        public void ParseApple_ReturnsExpectedTokens()
        {
            var credential = AuthCredentialBuilder.For(AuthType.Apple)
                .WithUsername("apple-user")
                .WithDisplayName("Apple User")
                .WithIdToken("apple-id-token")
                .WithRawNonce("nonce-value")
                .WithAccessToken("apple-access-token")
                .WithScopes(new[] { "email", "name" })
                .Build();

            var result = AuthCredentialBuilder.ParseApple(credential);

            Assert.AreEqual(AuthType.Apple, result.AuthType);
            Assert.AreEqual("apple-id-token", result.IdToken);
            Assert.AreEqual("nonce-value", result.RawNonce);
            Assert.AreEqual("apple-access-token", result.AccessToken);
            CollectionAssert.AreEquivalent(new[] { "email", "name" }, result.Scopes);
        }

        [Test]
        public void ParseWeb_UsesUsernameAsFallbackDisplayName()
        {
            var credential = AuthCredentialBuilder.For(AuthType.Google)
                .WithUsername("web-user")
                .WithScopes(new[] { "email " }) // spaced to test trimming
                .WithCustomParameters(new Dictionary<string, string> { { "display", "popup" } })
                .Build();

            var result = AuthCredentialBuilder.ParseWeb(credential);

            Assert.AreEqual("web-user", result.DisplayName);
            CollectionAssert.AreEquivalent(new[] { "email" }, result.Scopes);
            Assert.AreEqual("popup", result.CustomParameters["display"]);
        }

        [Test]
        public void ParseGoogle_Throws_WhenAuthTypeDoesNotMatch()
        {
            var credential = AuthCredentialBuilder.For(AuthType.Apple)
                .WithUsername("wrong")
                .Build();

            Assert.Throws<ArgumentException>(() => AuthCredentialBuilder.ParseGoogle(credential));
        }
    }
}
