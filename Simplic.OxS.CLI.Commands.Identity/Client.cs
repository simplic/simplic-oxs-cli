using CommonServiceLocator;
using Simplic.Studio.Ox;
using Simplic.Studio.Ox.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace Simplic.OxS.CLI.Identity
{
    /// <summary>
    /// Ox client containing various methods required by the CLI
    /// </summary>
    public class Client : IDisposable
    {
        private readonly ISharedIdRepository sharedIdRepository;
        private readonly IEnumerable<IInstanceDataUploadService> instanceDataUploadServices;

        private readonly HttpClient httpClient;
        private readonly AuthClient authClient;
        private readonly JwtSecurityTokenHandler handler = new();

        private readonly string email;
        private readonly string password;

        private bool disposed = false;

        /// <summary>
        /// Create new client without authorizing
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="email"></param>
        /// <param name="password"></param>
        public Client(Uri uri, string email, string password)
        {
            this.email = email;
            this.password = password;
            httpClient = new() { BaseAddress = uri };
            authClient = new(httpClient);

            sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();
            instanceDataUploadServices = ServiceLocator.Current.GetInstance<IEnumerable<IInstanceDataUploadService>>();
        }

        /// <summary>
        /// Create an account using the stored credentials
        /// </summary>
        /// <returns></returns>
        public async Task Register()
        {
            await authClient.RegisterAsync(new RegisterRequest
            {
                Email = email,
                Password = password,
            });
        }

        /// <summary>
        /// Login an account using the stored credentials.
        /// This must be called before accessing any Ox-related methods.
        /// </summary>
        /// <returns></returns>
        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Token) || !CheckJWTExpirationValid(Token))
                Token = await GetAuthorization();
        }

        /// <summary>
        /// Select an organization
        /// </summary>
        /// <param name="organizationId"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task LoginOrganization(Guid organizationId)
        {
            LoginResponse? authResponse;

            await Login();

            if (string.IsNullOrWhiteSpace(Token) || !CheckJWTExpirationValid(Token))
                Token = await GetAuthorization();
            if (GetOrganizationIdFromJWT(Token) == organizationId)
                return;

            authResponse = await authClient.SelectOrganizationAsync(new SelectOrganizationRequest
            {
                OrganizationId = organizationId
            });
            if (string.IsNullOrWhiteSpace(authResponse.Token))
                throw new Exception("Error during authentification");

            Token = authResponse.Token;
        }

        /// <summary>
        /// Change the password of the currently logged in account
        /// </summary>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task ChangePassword(string newPassword)
        {
            await Login();
            await authClient.ChangePasswordAsync(new ChangePasswordRequest
            {
                NewPassword = newPassword,
            });
        }

        /// <summary>
        /// Requests an auth token
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> GetAuthorization()
        {
            var authResponse = await authClient.LoginAsync(new LoginRequest()
            {
                Email = email,
                Password = password,
            });

            if (string.IsNullOrWhiteSpace(authResponse.Token))
                throw new Exception("Error during authentification");

            httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", authResponse.Token);

            return authResponse.Token;
        }

        /// <summary>
        /// Checks if a token is still valid or expired
        /// </summary>
        /// <param name="jwt"></param>
        /// <returns></returns>
        private bool CheckJWTExpirationValid(string jwt)
        {
            try
            {
                var jsonToken = handler.ReadToken(jwt);

                if (jsonToken is not JwtSecurityToken token)
                    return false;

                var exp = token?.Claims?.FirstOrDefault(x => x.Type == "exp")?.Value;
                var success = long.TryParse(exp, out var expLong);

                if (!success)
                    return false;

                var dateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(expLong);
                var expirationDate = dateTimeOffset.ToLocalTime();

                return expirationDate > DateTimeOffset.Now;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Extracts the organization id from a token
        /// </summary>
        /// <param name="jwt"></param>
        /// <returns></returns>
        private Guid? GetOrganizationIdFromJWT(string jwt)
        {
            try
            {
                var token = handler.ReadJwtToken(jwt);

                var oid = token?.Claims?.FirstOrDefault(x => x.Type == "OId")?.Value;
                var success = Guid.TryParse(oid, out var guid);

                if (!success)
                    return null;

                return guid;
            }
            catch
            {
                return null;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                httpClient.Dispose();
            }
            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Client()
        {
            Dispose(false);
        }

        public string? Token { get; private set; }

        public HttpClient HttpClient => httpClient;
    }
}
