using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BBIHardwareSupport
{
public class ProfileManager
    {
        private readonly AirWatchApiClient _apiClient;

        public ProfileManager(AirWatchApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<string> ReinstallProfileBySerialAsync(string deviceSerialNumber, int profileId)
        {
            string uri = $"https://as863.awmdm.com/API/mdm/profiles/{profileId}/install";
            string jsonContent = $"{{\"SerialNumber\": \"{deviceSerialNumber}\"}}";

            return await _apiClient.SendRequestAsync(uri, HttpMethod.Post, jsonContent);
        }
    }}
