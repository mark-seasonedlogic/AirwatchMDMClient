using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Management.Automation.Language;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport
{
    public class AirWatchApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly CredentialsManager _credentialsManager;

        public AirWatchApiClient(HttpClient httpClient, CredentialsManager credentialsManager)
        {
            _httpClient = httpClient;
            _credentialsManager = credentialsManager;
            ConfigureHttpClient();
        }

        public async Task<string> SendRequestAsync(string uri, HttpMethod method, string? content = null, string? accept = null)
        {
            string fullUri = $"{_httpClient.BaseAddress}{uri}";

            var request = new HttpRequestMessage(method, fullUri);
            request.Headers.Add("Authorization", _credentialsManager.GetAuthorizationHeader());
            request.Headers.Add("aw-tenant-code", _credentialsManager.TenantCode);
            request.Headers.Add("Accept", accept ?? "application/json");

            if (content != null)
            {
                request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            }

            var response = await _httpClient.SendAsync(request);
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                //do something with error here.
            }
            return await response.Content.ReadAsStringAsync();
        }

        private void ConfigureHttpClient()
        {
            _httpClient.BaseAddress = new Uri("https://as863.awmdm.com/API");
        }

        private async Task<List<JObject>> GetPagedResponseAsync(string endpoint, string itemType, Dictionary<string, string>? queryParams = null, string? accept = null)
        {
            var allResults = new List<JObject>();
            int currentPage = 0;
            int pageSize = 0;
            int totalItems = 0;

            do
            {

                queryParams ??= new Dictionary<string, string>();
                queryParams["page"] = currentPage.ToString();

                var queryString = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));
                var response = await SendRequestAsync($"{endpoint}?{queryString}", HttpMethod.Get, null, accept);
                if (String.IsNullOrEmpty(response))
                {
                    Debug.WriteLine("Trouble retrieving devices for user {0}", queryParams["user"]);
                    continue;
                }

                try
                {
                    //An array will not need to be paged, so check for this situation first
                    var jsonResponse = JToken.Parse(response);
                    JArray objectItems = JArray.Parse("[]");
                    if (jsonResponse is JObject)
                    {
                        //Take care of different versions
                        if (jsonResponse["page_size"] != null)
                        {
                            // Parse pagination metadata
                            pageSize = int.Parse(jsonResponse.Value<string>("page_size"));
                        }
                        else if (jsonResponse["PageSize"] != null)
                        {
                            pageSize = int.Parse(jsonResponse.Value<string>("PageSize"));
                        }
                        if (jsonResponse["total"] != null)
                        {
                            totalItems = int.Parse(jsonResponse.Value<string>("total"));
                        }
                        else if (jsonResponse["Total"] != null)
                        {
                            totalItems = int.Parse(jsonResponse.Value<string>("Total"));

                        }
                        objectItems = jsonResponse[itemType] as JArray; // Replace "Users" with the actual array key
                    }
                    switch (jsonResponse is JArray)
                    {
                        case true:
                            var items = jsonResponse;
                            // Extract items
                            if (items != null)
                            {
                                foreach (var item in items)
                                {
                                    allResults.Add((JObject)item);
                                }
                            }


                            break;
                        case false:
                            // Extract items
                            if (objectItems != null)
                            {
                                foreach (var item in objectItems)
                                {
                                    allResults.Add((JObject)item);
                                }
                            }
                            break;
                        default:
                            break;

                    }
                }
                catch (Exception ex)
                {
                    Debug.Print(ex.Message);
                }
                //page must start at zero!!
                currentPage++;

            } while ((currentPage) * pageSize < totalItems);

            return allResults;
        }
        public async Task<List<JObject>> GetOldDevicesByDaysAsync(int minDays)
        {
            DateTime lastSeenDate = DateTime.Now.AddDays(minDays * -1);
            var queryParams = new Dictionary<string, string> { { "lastseen", lastSeenDate.ToString() } };
            return await GetPagedResponseAsync("/mdm/devices/search", "Devices", queryParams, "application/json;version=2");
        }
        public async Task<List<JObject>> GetDevicesByUserAsync(string deviceUser)
        {
            var queryParams = new Dictionary<string, string> { { "user", deviceUser } };
            return await GetPagedResponseAsync("/mdm/devices/search", "devices", queryParams, "application/json;version=3");
        }
        public async Task<List<JObject>> GetAppsByAssignmentIdAsync(int groupId)
        {
            return await GetPagedResponseAsync($"/mdm/smartgroups/{groupId}/apps", "Devices", null, "application/json;version=2");
        }

        public async Task<List<JObject>> GetAppByNameAsync(string appName)
        {
            var queryParams = new Dictionary<string, string> { { "applicationname", appName } };
            return await GetPagedResponseAsync("/mam/apps/search", "Application", queryParams, "application/json");
        }

        public async Task<List<JObject>> GetAllAndroidDevicesAsync()
        {
            var queryParams = new Dictionary<string, string> { { "platform", "Android" } };
            return await GetPagedResponseAsync("/mdm/devices/search", "Devices", queryParams, "application/json;version=2");
        }
        public async Task<List<JObject>> GetAllAndroidDevicesByOrgExAsync(string orgId)
        {
            ///mdm/devices/extensivesearch?organizationgroupid={deviceUser}&platform=Android&page={page}"
            var queryParams = new Dictionary<string, string> { { "organizationgroupid", orgId }, { "platform", "Android" } };
            return await GetPagedResponseAsync("/mdm/devices/extensivesearch", "Devices", queryParams, "application/json;version=2");
        }
        public async Task<List<JObject>> GetAllUsernamesAsync()
        {
            return await GetPagedResponseAsync("/system/users/search", "Users", null, "application/json");
        }
    }
}
