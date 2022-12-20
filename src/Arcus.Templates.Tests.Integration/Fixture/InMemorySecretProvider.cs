using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.Security.Core;
using Arcus.Security.Core.Caching;
using Arcus.Security.Core.Caching.Configuration;
using GuardNet;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// <see cref="ISecretProvider"/> implementation that provides an in-memory storage of secrets by name.
    /// </summary>
    public class InMemorySecretProvider : ICachedSecretProvider, ISyncSecretProvider
    {
        private readonly IDictionary<string, string> _secretValueByName;

        /// <summary>
        /// Initializes a new instance of the <see cref="InMemorySecretProvider"/> class.
        /// </summary>
        /// <param name="secretValueByName">The sequence of combinations of secret names and values.</param>
        public InMemorySecretProvider(IDictionary<string, string> secretValueByName)
        {
            Guard.NotNull(secretValueByName, "Secret name/value combinations cannot be 'null'");

            _secretValueByName = secretValueByName;
        }

        /// <summary>
        /// Gets the cache-configuration for this instance.
        /// </summary>
        public ICacheConfiguration Configuration { get; } = null;

        /// <summary>
        /// Retrieves the secret value, based on the given name
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="Task{TResult}"/> that contains the secret key</returns>
        /// <exception cref="ArgumentException">The name must not be empty</exception>
        /// <exception cref="ArgumentNullException">The name must not be null</exception>
        public Task<string> GetRawSecretAsync(string secretName, bool ignoreCache)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            string secretValue = GetRawSecret(secretName);
            return Task.FromResult(secretValue);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <param name="ignoreCache">Indicates if the cache should be used or skipped</param>
        /// <returns>Returns a <see cref="T:System.Threading.Tasks.Task`1" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The name must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The name must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<Secret> GetSecretAsync(string secretName, bool ignoreCache)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            Secret secret = GetSecret(secretName);
            return Task.FromResult(secret);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<Secret> GetSecretAsync(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            Secret secret = GetSecret(secretName);
            return Task.FromResult(secret);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            string secretValue = GetRawSecret(secretName);
            return Task.FromResult(secretValue);
        }

        /// <summary>
        /// Removes the secret with the given <paramref name="secretName" /> from the cache;
        /// so the next time <see cref="M:Arcus.Security.Core.Caching.CachedSecretProvider.GetSecretAsync(System.String)" /> is called, a new version of the secret will be added back to the cache.
        /// </summary>
        /// <param name="secretName">The name of the secret that should be removed from the cache.</param>
        public Task InvalidateSecretAsync(string secretName)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Retrieves the secret value, based on the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the <paramref name="secretName" /> is blank.</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">Thrown when the secret was not found, using the given name.</exception>
        public Secret GetSecret(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            string secretValue = GetRawSecret(secretName);
            if (secretValue is null)
            {
                return null;
            }

            return new Secret(secretValue, "1.0.0");
        }

        /// <summary>
        /// Retrieves the secret value, based on the given name.
        /// </summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">Thrown when the <paramref name="secretName" /> is blank.</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">Thrown when the secret was not found, using the given name.</exception>
        public string GetRawSecret(string secretName)
        {
            Guard.NotNull(secretName, "Secret name cannot be 'null'");

            if (_secretValueByName.TryGetValue(secretName, out string secretValue))
            {
                return secretValue;
            }

            return null;
        }
    }
}
