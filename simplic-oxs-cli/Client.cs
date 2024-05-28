using CommonServiceLocator;
using Simplic.Studio.Ox;
using Simplic.Studio.Ox.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Simplic.Ox.CLI
{
    public class Client : IDisposable
    {
        private readonly ISharedIdRepository sharedIdRepository;
        private readonly IEnumerable<IInstanceDataUploadService> instanceDataUploadServices;

        private readonly HttpClient httpClient;
        private readonly AuthClient authClient;
        private readonly OrganizationClient organizationClient;
        private readonly JwtSecurityTokenHandler handler = new();

        private readonly string email;
        private readonly string password;

        private bool disposed = false;

        public Client(Uri uri, string email, string password)
        {
            this.email = email;
            this.password = password;
            httpClient = new()
            {
                BaseAddress = uri
            };
            sharedIdRepository = ServiceLocator.Current.GetInstance<ISharedIdRepository>();
            instanceDataUploadServices = ServiceLocator.Current.GetInstance<IEnumerable<IInstanceDataUploadService>>();
            authClient = new(httpClient);
            organizationClient = new(httpClient);
        }

        public async Task Register()
        {
            await authClient.RegisterAsync(new RegisterRequest
            {
                Email = email,
                Password = password,
            });
        }

        public async Task Login()
        {
            if (string.IsNullOrWhiteSpace(Token) || !CheckJWTExpirationValid(Token))
                Token = await GetAuthorization();
        }

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

        public Task<OrganizationModel> CreateDummyOrganization(string name, AddressModel address) =>
            organizationClient.OrganizationPostAsync(new CreateOrganizationRequest
            {
                Name = name,
                Address = address,
                Dummy = true,
            });

        public async Task<OrganizationMemberModel?> GetOrganizationById(Guid id)
        {
            var organizations = await ListOrganizations();
            return organizations.FirstOrDefault(o => o.OrganizationId == id);
        }

        public async Task<OrganizationMemberModel?> GetOrganizationByName(string name)
        {
            var organizations = await ListOrganizations();
            return organizations.FirstOrDefault(o => o.OrganizationName == name);
        }

        public Task<ICollection<OrganizationMemberModel>> ListOrganizations() => organizationClient.GetForUserAsync();

        public Task DeleteOrganization(Guid id) => organizationClient.OrganizationDeleteAsync(id);

        public async Task UploadData(Guid id, string context, Guid tenantId)
        {
            var service = instanceDataUploadServices.FirstOrDefault(x => x.ContextName == context);

            if (service == null)
            {
                Console.WriteLine($"No oxs-service found for context: {context}");
                return;
            }

            var sharedId = await sharedIdRepository.GetSharedIdByStudioId(id, context, tenantId);

            Console.WriteLine($"Upload to oxs: {sharedId.Context} / instance data guid: {sharedId.InstanceDataId} / oxs-id: {sharedId.OxSId} / tenant-id: {tenantId}");

            // Call create method when either no entry for a shared id is found or the oxs id in empty.
            if (sharedId == null || sharedId.OxSId == Guid.Empty)
            {
                Console.WriteLine($"  Creating...");

                var oxsId = await service.CreateUpload(id, Token);

                if (oxsId == default)
                {
                    Console.WriteLine($"  > Invalid OxS ID received, skipping");
                    return;
                }

                sharedId = new SharedId
                {
                    InstanceDataId = id,
                    OxSId = oxsId,
                    Context = context,
                    TenantId = tenantId
                };

                await sharedIdRepository.Save(sharedId);

                Console.WriteLine($"  > Done");
                return;
            }

            Console.WriteLine($"  Updating...");

            // Call update method in the other case.
            await service.UpdateUpload(sharedId.InstanceDataId, sharedId.OxSId, Token);
            Console.WriteLine($"  > Done");
        }

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
    }
}
