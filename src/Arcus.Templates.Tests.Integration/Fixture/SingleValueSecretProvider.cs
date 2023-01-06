using System.Threading.Tasks;
using Arcus.Security.Core;

namespace Arcus.Templates.Tests.Integration.Fixture
{
    /// <summary>
    /// Represents a <see cref="ISecretProvider"/> that only returns a single secret value, regardless of the secret name.
    /// </summary>
    public class SingleValueSecretProvider : ISecretProvider
    {
        private readonly string _value;

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleValueSecretProvider"/> class.
        /// </summary>
        public SingleValueSecretProvider(string value)
        {
            _value = value;
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns the secret key.</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<string> GetRawSecretAsync(string secretName)
        {
            return Task.FromResult(_value);
        }

        /// <summary>Retrieves the secret value, based on the given name</summary>
        /// <param name="secretName">The name of the secret key</param>
        /// <returns>Returns a <see cref="T:Arcus.Security.Core.Secret" /> that contains the secret key</returns>
        /// <exception cref="T:System.ArgumentException">The <paramref name="secretName" /> must not be empty</exception>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="secretName" /> must not be null</exception>
        /// <exception cref="T:Arcus.Security.Core.SecretNotFoundException">The secret was not found, using the given name</exception>
        public Task<Secret> GetSecretAsync(string secretName)
        {
            var secret = new Secret(_value, "1.0.0");
            return Task.FromResult(secret);
        }
    }
}
