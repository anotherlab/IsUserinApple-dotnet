using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace rajapet.Apple
{
    public enum Role { AccessToReports, AccountHolder, Admin, AppManager, CustomerSupport, Developer, Finance, Marketing, Sales };
    public enum TypeEnum { Users };

    /// <summary>
    /// Service class for making App Store Connect API calls
    /// </summary>
    public class AppStoreApiService
    {
        private HttpClient _client;
        public HttpClient client {
            get {
                if (_client == null)
                {
                    _client = new HttpClient();    
                    _client.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", token);
                }

                return _client;
            }
        }

        public string token {get; set;}

        public AppStoreApiService(string token)
        {
            this.token = token;
        }

        /// <summary>
        /// Find a user in the App Store account by email address
        /// </summary>
        /// <param name="EmailAddress"></param>
        /// <returns>A matching <see cref="User"></returns>
        public User FindUser(string EmailAddress)
        {
            var users = GetAllUsers();

            if (users != null)
            {
                var user = users.Where(s => s.UserName.Equals(EmailAddress, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();

                return user;
            }

            return null;
        }

        /// <summary>
        /// Retrieves the App Store user list
        /// </summary>
        /// <returns></returns>
        public List<User> GetAllUsers()
        {
	        List<User> users = new List<User>();

           	var jsonString = GetUsers(token, 100, null).Result;

            if (jsonString == null)
            {
                return null;
            }
            var appConnectUsers = AppConnectUsers.FromJson(jsonString);
            
            
            users.AddRange(	appConnectUsers.Data
                .Select(s => s.Attributes)
                .Select(s => new User() {UserName = s.Username, 
                                         LastName = s.LastName, 
                                         FirstName = s.FirstName, 
                                         Roles = s.Roles.ToList()}) );
            
            while (appConnectUsers.Links.Next != null)
            {
                jsonString = GetUsers(token, 100, appConnectUsers.Links.Next.ToString()).Result;
                appConnectUsers = AppConnectUsers.FromJson(jsonString);
                users.AddRange(appConnectUsers.Data
                     .Select(s => s.Attributes)
                     .Select(s => new User() { UserName = s.Username, 
                                               LastName = s.LastName, 
                                               FirstName = s.FirstName, 
                                               Roles = s.Roles.ToList() }));
            }

            return users;
        }

        private async Task<string> GetUsers(string token, int count, string nextUrl)
        {
            var url = nextUrl ?? $"https://api.appstoreconnect.apple.com/v1/users?limit={count}";
                
            var result = await client.GetAsync(url);
            
            if (result.StatusCode == System.Net.HttpStatusCode.InternalServerError)
            {
                Console.WriteLine(result.StatusCode.ToString());
                return null;
            }
            else
            {
                var users = result.Content.ReadAsStringAsync();

                return users.Result;
            }
        }

    }

    public class User
    {
        
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<Role> Roles { get; set; } = new List<Role>();
    }


    public partial class AppConnectUsers
    {
        [JsonProperty("data")]
        public Datum[] Data { get; set; }

        [JsonProperty("links")]
        public AppConnectUsersLinks Links { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }

	    public static AppConnectUsers FromJson(string json) => JsonConvert.DeserializeObject<AppConnectUsers>(json, Converter.Settings);

    }
    public partial class AppConnectUsersLinks
    {
        [JsonProperty("self")]
        public Uri Self { get; set; }

        [JsonProperty("next")]
        public Uri Next { get; set; }
    }
    public partial class Datum
    {
        [JsonProperty("type")]
        public TypeEnum Type { get; set; }

        [JsonProperty("id")]
        public Guid Id { get; set; }

        [JsonProperty("attributes")]
        public Attributes Attributes { get; set; }

        [JsonProperty("relationships")]
        public Relationships Relationships { get; set; }

        [JsonProperty("links")]
        public DatumLinks Links { get; set; }
    }

    public partial class Attributes
    {
        [JsonProperty("username")]
        public string Username { get; set; }

        [JsonProperty("firstName")]
        public string FirstName { get; set; }

        [JsonProperty("lastName")]
        public string LastName { get; set; }

        [JsonProperty("roles")]
        public Role[] Roles { get; set; }

        [JsonProperty("allAppsVisible")]
        public bool AllAppsVisible { get; set; }

        [JsonProperty("provisioningAllowed")]
        public bool ProvisioningAllowed { get; set; }
    }

    public partial class DatumLinks
    {
        [JsonProperty("self")]
        public Uri Self { get; set; }
    }
    public partial class Relationships
    {
        [JsonProperty("visibleApps")]
        public VisibleApps VisibleApps { get; set; }
    }
    public partial class VisibleApps
    {
        [JsonProperty("links")]
        public VisibleAppsLinks Links { get; set; }
    }

    public partial class VisibleAppsLinks
    {
        [JsonProperty("self")]
        public Uri Self { get; set; }

        [JsonProperty("related")]
        public Uri Related { get; set; }
    }

    public partial class Meta
    {
        [JsonProperty("paging")]
        public Paging Paging { get; set; }
    }

    public partial class Paging
    {
        [JsonProperty("total")]
        public long Total { get; set; }

        [JsonProperty("limit")]
        public long Limit { get; set; }
    }


    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
                {
                    RoleConverter.Singleton,
                    TypeEnumConverter.Singleton,
                    new IsoDateTimeConverter { DateTimeStyles = DateTimeStyles.AssumeUniversal }
                },
        };
    }

    internal class RoleConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(Role) || t == typeof(Role?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            switch (value)
            {
                case "ACCESS_TO_REPORTS":
                    return Role.AccessToReports;
                case "ACCOUNT_HOLDER":
                    return Role.AccountHolder;
                case "ADMIN":
                    return Role.Admin;
                case "APP_MANAGER":
                    return Role.AppManager;
                case "CUSTOMER_SUPPORT":
                    return Role.CustomerSupport;
                case "DEVELOPER":
                    return Role.Developer;
                case "FINANCE":
                    return Role.Finance;
                case "MARKETING":
                    return Role.Marketing;
                case "SALES":
                    return Role.Sales;
            }
            throw new Exception("Cannot unmarshal type Role");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (Role)untypedValue;
            switch (value)
            {
                case Role.AccessToReports:
                    serializer.Serialize(writer, "ACCESS_TO_REPORTS");
                    return;
                case Role.AccountHolder:
                    serializer.Serialize(writer, "ACCOUNT_HOLDER");
                    return;
                case Role.Admin:
                    serializer.Serialize(writer, "ADMIN");
                    return;
                case Role.AppManager:
                    serializer.Serialize(writer, "APP_MANAGER");
                    return;
                case Role.CustomerSupport:
                    serializer.Serialize(writer, "CUSTOMER_SUPPORT");
                    return;
                case Role.Developer:
                    serializer.Serialize(writer, "DEVELOPER");
                    return;
                case Role.Finance:
                    serializer.Serialize(writer, "FINANCE");
                    return;
                case Role.Marketing:
                    serializer.Serialize(writer, "MARKETING");
                    return;
                case Role.Sales:
                    serializer.Serialize(writer, "SALES");
                    return;
            }
            throw new Exception("Cannot marshal type Role");
        }

        public static readonly RoleConverter Singleton = new RoleConverter();
    }

    internal class TypeEnumConverter : JsonConverter
    {
        public override bool CanConvert(Type t) => t == typeof(TypeEnum) || t == typeof(TypeEnum?);

        public override object ReadJson(JsonReader reader, Type t, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var value = serializer.Deserialize<string>(reader);
            if (value == "users")
            {
                return TypeEnum.Users;
            }
            throw new Exception("Cannot unmarshal type TypeEnum");
        }

        public override void WriteJson(JsonWriter writer, object untypedValue, JsonSerializer serializer)
        {
            if (untypedValue == null)
            {
                serializer.Serialize(writer, null);
                return;
            }
            var value = (TypeEnum)untypedValue;
            if (value == TypeEnum.Users)
            {
                serializer.Serialize(writer, "users");
                return;
            }
            throw new Exception("Cannot marshal type TypeEnum");
        }

        public static readonly TypeEnumConverter Singleton = new TypeEnumConverter();
    }


}