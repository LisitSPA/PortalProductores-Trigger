using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace PortalProductores_Trigger
{
    public class Integrador
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [FunctionName("Scheduler")]
        public async Task Run([TimerTrigger("0 40 7,15,19,23 * * *")] TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"Function ejecutada a las: {DateTime.Now}");

            // Endpoint para obtener el token
            var tokenEndpoint = Environment.GetEnvironmentVariable("TOKEN_API_ENDPOINT_URL");
            if (string.IsNullOrEmpty(tokenEndpoint))
            {
                log.LogError("TOKEN_API_ENDPOINT_URL no ha sido encontrado en las variables de entorno.");
                return;
            }

            // Endpoint para usar el token
            var securedEndpoint = Environment.GetEnvironmentVariable("SECURED_API_ENDPOINT_URL");
            if (string.IsNullOrEmpty(securedEndpoint))
            {
                log.LogError("SECURED_API_ENDPOINT_URL no ha sido encontrado en las variables de entorno.");
                return;
            }

            // Paso 1: Obtener el token
            var tokenPayload = new
            {
                username = "admin@lisit.cl",
                password = "d4c6aaea504e5a3a6ce904a6bc953f46bfc5ad542845e7943007b7b2fefe3a15"
            };

            var tokenPayloadJson = JsonSerializer.Serialize(tokenPayload);
            var tokenContent = new StringContent(tokenPayloadJson, Encoding.UTF8, "application/json");

            try
            {
                var tokenResponse = await HttpClient.PostAsync(tokenEndpoint, tokenContent);
                if (!tokenResponse.IsSuccessStatusCode)
                {
                    log.LogError($"Fail al obtener el token: {tokenResponse.StatusCode} - {tokenResponse.ReasonPhrase}");
                    return;
                }

                var tokenResponseContent = await tokenResponse.Content.ReadAsStringAsync();
                var tokenResponseObject = JsonSerializer.Deserialize<ResponseModel>(tokenResponseContent);

                if (tokenResponseObject == null || string.IsNullOrEmpty(tokenResponseObject.Token))
                {
                    log.LogError("No se ha podido extraer el token.");
                    return;
                }
                var token = tokenResponseObject.Token;
                log.LogInformation($"Token obtenido: {token}");

                // Paso 2: Usar el token en el siguiente POST
                HttpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var securedPayload = new
                {
                    anoTemporada = 0,
                    enviaCorreo = 1
                };

                var securedPayloadJson = JsonSerializer.Serialize(securedPayload);
                var securedContent = new StringContent(securedPayloadJson, Encoding.UTF8, "application/json");

                var securedResponse = await HttpClient.PostAsync(securedEndpoint, securedContent);
                if (securedResponse.IsSuccessStatusCode)
                {
                    log.LogInformation($"Secured POST response: {securedResponse.StatusCode}");
                }
                else
                {
                    log.LogError($"Secured POST request falló con INFO: {securedResponse.StatusCode} - {securedResponse.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                log.LogError($"Error sending POST request: {ex.Message}");
            }
        }
    }
}


public sealed class ResponseModel
{
    public int Id { get; set; }
    public string Nombre { get; set; }
    public string Correo { get; set; }
    public string Telefono { get; set; }
    public int Perfil { get; set; }
    public string PerfilDescripcion { get; set; }
    public int Estado { get; set; }
    public string EstadoDescripcion { get; set; }
    public bool ContrasenaGenerica { get; set; }
    public string Token { get; set; }
}