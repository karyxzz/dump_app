using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Snowflake.Data.Client;
using System.Text.Json;

namespace SnowflakeFunction
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("GetData")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req)
        {
            // Get table name from query parameter
            string tableName = req.Query["table"] ?? "customer";

            try
            {
                // Get connection string from settings
                string connectionString = Environment.GetEnvironmentVariable("SNOWFLAKE_CONNECTION");

                // Connect and get data
                using var connection = new SnowflakeDbConnection(connectionString);
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = $"SELECT * FROM {tableName} LIMIT 10";

                using var reader = await command.ExecuteReaderAsync();

                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);

                    // Print to console/logs
                    _logger.LogInformation($"Row: {JsonSerializer.Serialize(row)}");
                }

                // Return response
                var response = req.CreateResponse(System.Net.HttpStatusCode.OK);
                await response.WriteStringAsync(JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error: {ex.Message}");
                var errorResponse = req.CreateResponse(System.Net.HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }
    }
}
