using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using VideoOS.Platform;
using VideoOS.Platform.Login;
using VideoOS.Platform.ConfigurationItems;
using VideoOS.Platform.OAuth; // Thêm cái này để test Config API


namespace LightInsight.Dashboard.Camera
{
	internal class Api
	{
		public string GetMilestoneAccessToken()
		{
			var serverId = EnvironmentManager.Instance.MasterSite.ServerId;
			var settings = LoginSettingsCache.GetLoginSettings(serverId);
			string token = settings.IdentityTokenCache.Token;

			System.Diagnostics.Debug.WriteLine($"[API TEST] Server ID: {serverId.Id}");
			System.Diagnostics.Debug.WriteLine($"[API TEST] Token: {token}"); // Chỉ in 1 đoạn để bảo mật

			return token.ToString();
		}

		public async Task TestRestApiCall()
		{
			try
			{
				string token = GetMilestoneAccessToken();
				var serverUri = EnvironmentManager.Instance.MasterSite.ServerId.Uri;

				// Endpoint chuẩn của Milestone REST API (v1)
				// Lưu ý: Milestone REST API thường chạy trên cổng 443 hoặc 80 của Management Server
				string apiUrl = $"{serverUri.Scheme}://{serverUri.Host}/api/rest/v1/cameras";

				using (var client = new HttpClient())
				{
					// 1. Đính kèm Token vào Header
					client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));


					System.Diagnostics.Debug.WriteLine($"[API TEST] Calling: {apiUrl}");


					// 2. Thực hiện gọi API
					var response = await client.GetAsync(apiUrl);


					// 3. Đọc kết quả
					if (response.IsSuccessStatusCode)
					{
						string jsonResult = await response.Content.ReadAsStringAsync();
						System.Diagnostics.Debug.WriteLine("[API TEST] SUCCESS! Data received.");
						System.Diagnostics.Debug.WriteLine(jsonResult.Substring(0, Math.Min(500, jsonResult.Length)));
					}
					else
					{
						System.Diagnostics.Debug.WriteLine($"[API TEST] FAILED. Status: {response.StatusCode}");
					}
				}
			}
			catch (Exception ex)
			{
				System.Diagnostics.Debug.WriteLine($"[API TEST] ERROR: {ex.Message}");
			}
		}
	}
}