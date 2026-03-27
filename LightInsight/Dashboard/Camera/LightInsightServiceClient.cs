using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

namespace LightInsight.Dashboard.Camera
{
    public class LightInsightServiceClient
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private string _baseUrl = "http://192.168.100.5:4000"; // Phải khớp với Port trong appsettings.json của Service

        public LightInsightServiceClient(string host = "192.168.100.5", int port = 4000)
        {
            _baseUrl = $"http://{host}:{port}";
        }

        /// <summary>
        /// Gọi API lấy dữ liệu từ LightInsightService
        /// </summary>
        public async Task<string> GetDataAsync(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/api/{endpoint}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[ServiceClient] Lỗi API: {response.StatusCode}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ServiceClient] Lỗi kết nối Service: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Ví dụ: Lấy danh sách camera từ Database thông qua Service
        /// </summary>
        public async Task<T> GetAsync<T>(string endpoint)
        {
            string json = await GetDataAsync(endpoint);
            if (string.IsNullOrEmpty(json)) return default;

            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    }
}
