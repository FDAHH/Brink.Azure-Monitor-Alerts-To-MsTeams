using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Azure.Monitor.Query;
using AzureMonitorAlertToTeams.Configurations;
using AzureMonitorAlertToTeams.Models;
using AzureMonitorAlertToTeams.QueryResultFetchers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureMonitorAlertToTeams.AlertProcessors.LogAnalytics
{
    #warning TODO: should use system assign identity and probalby there is a package or lib? https://learn.microsoft.com/en-us/dotnet/api/overview/azure/monitor.query-readme?view=azure-dotnet
    public interface ILogAnalyticsQueryResultFetcher : IQueryResultFetcher
    {
    };

    public class LogAnalyticsQueryResultFetcher : ILogAnalyticsQueryResultFetcher
    {
        private readonly HttpClient _httpClient;
        private readonly LogsQueryClient _logsQueryClient;

        private readonly ILogger _log;

        public LogAnalyticsQueryResultFetcher(ILogger<LogAnalyticsQueryResultFetcher> log, IHttpClientFactory httpClientFactory, LogsQueryClient logsQueryClient)
        {
            _log = log;
            _logsQueryClient = logsQueryClient;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<ResultSet> FetchLogQueryResultsAsync(string url, string jsonConfiguration)
        {
            
            var configuration = JsonConvert.DeserializeObject<LogAnalyticsConfiguration>(jsonConfiguration);

            if (configuration?.ClientId == null)
                throw new InvalidOperationException("Cannot get ClientId from configuration {jsonConfiguration}");

            var formData = new Dictionary<string, string>
            {
                {"client_id", configuration.ClientId},
                {"redirect_uri", configuration.RedirectUrl},
                {"grant_type", "client_credentials"},
                {"client_secret", configuration.ClientSecret},
                {"resource", "https://api.loganalytics.io"}
            };

            var postResponse = await _httpClient.PostAsync($"https://login.microsoftonline.com/{configuration.TenantId}/oauth2/token", new FormUrlEncodedContent(formData));
            var tokenData = await postResponse.Content.ReadAsStringAsync();
            if (!postResponse.IsSuccessStatusCode)
                throw new HttpRequestException(tokenData);

            var token = JsonConvert.DeserializeObject<dynamic>(tokenData);
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", (string)token.access_token);

            var rawResult = await _httpClient.GetStringAsync(url);

            _log.LogDebug($"Data received: {rawResult}");

            var result = JsonConvert.DeserializeObject<ResultSet>(rawResult);
            return result;
        }
    }
}