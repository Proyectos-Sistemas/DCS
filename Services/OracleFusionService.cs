using Homer_MVC.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Homer_MVC.Services
{
    public class OracleFusionService
    {
        // UTF8Encoding(false): Encoding.UTF8 emits a BOM via GetPreamble(), which StringContent
        // writes at the start of the body. Fusion's serviceRequests parser rejects that BOM with
        // "Unable to parse the provided payload" even though contacts/accounts tolerate it.
        private static readonly UTF8Encoding JsonEncoding = new UTF8Encoding(false);
        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        private readonly string _baseUrl;
        private readonly string _username;
        private readonly string _password;

        public OracleFusionService()
        {
            _baseUrl = ConfigurationManager.AppSettings["OracleFusion:BaseUrl"] ?? "";
            _username = ConfigurationManager.AppSettings["OracleFusion:Username"] ?? "";
            _password = ConfigurationManager.AppSettings["OracleFusion:Password"] ?? "";
        }

        /// <summary>
        /// Header HTTP Basic (usuario:password de Fusion en base64). Solo para pruebas;
        /// producción debería usar OAuth2 contra el dominio de identidad (IDCS/OCI IAM),
        /// no la URL de la app Fusion.
        /// </summary>
        private string GetBasicAuthHeader()
        {
            var raw = Encoding.UTF8.GetBytes($"{_username}:{_password}");
            return "Basic " + Convert.ToBase64String(raw);
        }

        /// <summary>
        /// Busca si existe una cuenta por email o número de identificación
        /// </summary>
        public async Task<OracleSearchItem> SearchAccountAsync(string email, string nationalId = null)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    // Buscar por email
                    var searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts?q=EmailAddress='{Uri.EscapeDataString(email)}'";
                    
                    var response = await client.GetAsync(searchUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var searchResponse = JsonConvert.DeserializeObject<OracleSearchResponse>(content);
                        
                        if (searchResponse?.Items != null && searchResponse.Items.Count > 0)
                        {
                            return searchResponse.Items[0];
                        }
                    }

                    // Si no se encuentra por email y hay nationalId, buscar por ese campo
                    if (!string.IsNullOrEmpty(nationalId))
                    {
                        searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts?q=NationalId='{Uri.EscapeDataString(nationalId)}'";
                        response = await client.GetAsync(searchUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            var content = await response.Content.ReadAsStringAsync();
                            var searchResponse = JsonConvert.DeserializeObject<OracleSearchResponse>(content);
                            
                            if (searchResponse?.Items != null && searchResponse.Items.Count > 0)
                            {
                                return searchResponse.Items[0];
                            }
                        }
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar cuenta en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca un contacto por email. Un error de Fusion se lanza en vez de tratarse como
        /// "no encontrado", para no crear contactos duplicados. (NationalId no es consultable
        /// con q= en esta instancia: Fusion responde 400.)
        /// </summary>
        public async Task<OracleSearchItem> SearchContactAsync(string email)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/contacts?q=EmailAddress='{Uri.EscapeDataString(email)}'&fields=PartyNumber,PartyId&limit=1";
                    var response = await client.GetAsync(searchUrl);
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Fusion respondio {(int)response.StatusCode}: {content}");
                    }

                    var searchResponse = JsonConvert.DeserializeObject<OracleSearchResponse>(content);
                    return searchResponse?.Items != null && searchResponse.Items.Count > 0 ? searchResponse.Items[0] : null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar contacto en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea una cuenta en Oracle Fusion
        /// </summary>
        public async Task<OracleAccountResponse> CreateAccountAsync(OracleAccount account)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var accountJson = JsonConvert.SerializeObject(account, JsonSettings);
                    var content = new StringContent(accountJson, JsonEncoding, "application/json");

                    var createUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts";
                    var response = await client.PostAsync(createUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Fusion respondio {(int)response.StatusCode}: {responseContent}");
                    }

                    var accountResponse = JsonConvert.DeserializeObject<OracleAccountResponse>(responseContent);

                    return accountResponse;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear cuenta en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea un contacto en Oracle Fusion
        /// </summary>
        public async Task<OracleContactResponse> CreateContactAsync(OracleContact contact)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var contactJson = JsonConvert.SerializeObject(contact, JsonSettings);
                    var content = new StringContent(contactJson, JsonEncoding, "application/json");

                    var createUrl = $"{_baseUrl}/crmRestApi/resources/latest/contacts";
                    var response = await client.PostAsync(createUrl, content);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var contactResponse = JsonConvert.DeserializeObject<OracleContactResponse>(responseContent);

                    return contactResponse;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear contacto en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea un Service Request en Oracle Fusion
        /// </summary>
        public async Task<OracleServiceRequestResponse> CreateServiceRequestAsync(OracleServiceRequest sr)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var srJson = JsonConvert.SerializeObject(sr, JsonSettings);
                    var content = new StringContent(srJson, JsonEncoding, "application/json");

                    var createUrl = $"{_baseUrl}/crmRestApi/resources/latest/serviceRequests";
                    var response = await client.PostAsync(createUrl, content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Fusion respondio {(int)response.StatusCode}: {responseContent}");
                    }

                    return JsonConvert.DeserializeObject<OracleServiceRequestResponse>(responseContent);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear Service Request en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PATCH de Cuenta y Contacto principal en un SR existente. Usa los PartyId numericos:
        /// en el describe los *PartyNumber son allowChanges=never (solo se pueden fijar al crear).
        /// </summary>
        public async Task UpdateServiceRequestPartiesAsync(string srNumber, long contactPartyId, long accountPartyId)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var updateJson = JsonConvert.SerializeObject(new
                    {
                        PrimaryContactPartyId = contactPartyId,
                        AccountPartyId = accountPartyId
                    }, JsonSettings);
                    var content = new StringContent(updateJson, JsonEncoding, "application/json");

                    var updateUrl = $"{_baseUrl}/crmRestApi/resources/latest/serviceRequests/{srNumber}";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl) { Content = content };
                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Fusion respondio {(int)response.StatusCode}: {responseContent}");
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al actualizar el Service Request {srNumber} en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Tag que va en el asunto del correo. Fusion copia el asunto al Title del SR, asi que es
        /// la unica forma de encontrar el SR que crea el correo entrante. El "]" final evita que
        /// [DCS-12] coincida con [DCS-123].
        /// </summary>
        public static string SolicitudTag(int idSolicitud)
        {
            return $"[DCS-{idSolicitud}]";
        }

        /// <summary>
        /// Devuelve el SrNumber mas reciente cuyo Title contiene el tag, o null si Fusion aun no
        /// lo ha creado. Un error de Fusion se lanza en vez de tratarse como "no encontrado".
        /// </summary>
        public async Task<string> FindServiceRequestByTitleTagAsync(string tag)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    var q = Uri.EscapeDataString($"Title LIKE '%{tag}%'");
                    var searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/serviceRequests?q={q}&fields=SrNumber&orderBy=CreationDate:desc&limit=1";
                    var response = await client.GetAsync(searchUrl);
                    var content = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Fusion respondio {(int)response.StatusCode}: {content}");
                    }

                    var items = JsonConvert.DeserializeObject<OracleServiceRequestSearchResponse>(content)?.Items;
                    return items != null && items.Count > 0 ? items[0].SrNumber : null;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al buscar Service Request {tag} en Oracle Fusion: {ex.Message}", ex);
            }
        }

        private async Task<OracleSearchItem> EnsureContactAsync(string firstName, string lastName, string email, string nationalId)
        {
            var existing = await SearchContactAsync(email);
            if (existing != null)
            {
                return existing;
            }

            var created = await CreateContactAsync(new OracleContact
            {
                PersonFirstName = firstName,
                PersonLastName = lastName,
                EmailAddress = email,
                NationalId = nationalId ?? "",
                NationalIdType = "CUI"
            });
            return new OracleSearchItem { PartyNumber = created.PartyNumber, PartyId = created.PartyId };
        }

        /// <summary>
        /// Busca la cuenta cuyo contacto principal es el solicitante. Las cuentas que crea DCS no
        /// guardan email ni CUI, asi que buscarlas por esos campos nunca las encontraba y se
        /// creaba una cuenta duplicada en cada solicitud.
        /// </summary>
        private async Task<OracleSearchItem> SearchAccountByPrimaryContactAsync(string contactPartyNumber)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", GetBasicAuthHeader());
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                var searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts?q=PrimaryContactPartyNumber='{Uri.EscapeDataString(contactPartyNumber)}'&fields=PartyNumber,PartyId&limit=1";
                var response = await client.GetAsync(searchUrl);
                var content = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Error al buscar cuenta por contacto principal en Oracle Fusion: Fusion respondio {(int)response.StatusCode}: {content}");
                }

                var searchResponse = JsonConvert.DeserializeObject<OracleSearchResponse>(content);
                return searchResponse?.Items != null && searchResponse.Items.Count > 0 ? searchResponse.Items[0] : null;
            }
        }

        private async Task<OracleSearchItem> EnsureAccountAsync(string firstName, string lastName, string email, string nationalId, string contactPartyNumber)
        {
            var existing = await SearchAccountByPrimaryContactAsync(contactPartyNumber)
                ?? await SearchAccountAsync(email, nationalId);
            if (existing != null)
            {
                return existing;
            }

            var created = await CreateAccountAsync(new OracleAccount
            {
                OrganizationName = $"{firstName} {lastName}",
                PrimaryContactPartyNumber = contactPartyNumber
            });
            return new OracleSearchItem { PartyNumber = created.PartyNumber, PartyId = created.PartyId };
        }

        /// <summary>
        /// Completa un SR ya creado (en prod, por el correo entrante) con lo que el correo no llena:
        /// Contacto principal y Cuenta, creandolos en Fusion si no existen.
        /// </summary>
        public async Task UpdateServiceRequestForSolicitudAsync(
            string srNumber,
            string firstName,
            string lastName,
            string email,
            string nationalId)
        {
            var contact = await EnsureContactAsync(firstName, lastName, email, nationalId);
            var account = await EnsureAccountAsync(firstName, lastName, email, nationalId, contact.PartyNumber);
            await UpdateServiceRequestPartiesAsync(srNumber, long.Parse(contact.PartyId), long.Parse(account.PartyId));
        }

        /// <summary>
        /// Solo para el ambiente de pruebas, donde el correo entrante no crea SRs: crea el SR con los
        /// mismos campos que llenaria el correo y luego aplica el mismo update que se usara en prod.
        /// Devuelve el SrNumber creado.
        /// </summary>
        public async Task<string> CreateServiceRequestForSolicitudAsync(
            string firstName,
            string lastName,
            string email,
            string nationalId,
            string title,
            string description)
        {
            var srResponse = await CreateServiceRequestAsync(new OracleServiceRequest
            {
                Title = title,
                ProblemDescription = description != null && description.Length > 1000
                    ? description.Substring(0, 1000)
                    : description
            });

            await UpdateServiceRequestForSolicitudAsync(srResponse.SrNumber, firstName, lastName, email, nationalId);

            return srResponse.SrNumber;
        }
    }
}

