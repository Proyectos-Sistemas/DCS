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
        private readonly string _baseUrl;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _username;
        private readonly string _password;
        private readonly string _scope;
        private string _accessToken;
        private DateTime _tokenExpiry;

        public OracleFusionService()
        {
            _baseUrl = ConfigurationManager.AppSettings["OracleFusion:BaseUrl"] ?? "";
            _clientId = ConfigurationManager.AppSettings["OracleFusion:ClientId"] ?? "";
            _clientSecret = ConfigurationManager.AppSettings["OracleFusion:ClientSecret"] ?? "";
            _username = ConfigurationManager.AppSettings["OracleFusion:Username"] ?? "";
            _password = ConfigurationManager.AppSettings["OracleFusion:Password"] ?? "";
            _scope = ConfigurationManager.AppSettings["OracleFusion:Scope"] ?? "crmRestApi";
        }

        /// <summary>
        /// Obtiene un token de acceso OAuth2 de Oracle Fusion
        /// </summary>
        private async Task<string> GetAccessTokenAsync()
        {
            // Si el token existe y no ha expirado, retornarlo
            if (!string.IsNullOrEmpty(_accessToken) && DateTime.Now < _tokenExpiry)
            {
                return _accessToken;
            }

            try
            {
                using (var client = new HttpClient())
                {
                    var tokenUrl = $"{_baseUrl}/oauth2/v1/token";
                    var requestContent = new FormUrlEncodedContent(new[]
                    {
                        new KeyValuePair<string, string>("grant_type", "password"),
                        new KeyValuePair<string, string>("username", _username),
                        new KeyValuePair<string, string>("password", _password),
                        new KeyValuePair<string, string>("client_id", _clientId),
                        new KeyValuePair<string, string>("client_secret", _clientSecret),
                        new KeyValuePair<string, string>("scope", _scope)
                    });

                    var response = await client.PostAsync(tokenUrl, requestContent);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
                    var tokenResponse = JsonConvert.DeserializeObject<OracleOAuthResponse>(responseContent);

                    _accessToken = tokenResponse.AccessToken;
                    _tokenExpiry = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn - 60); // Restar 60 segundos como margen

                    return _accessToken;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al obtener token de acceso de Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Busca si existe una cuenta por email o número de identificación
        /// </summary>
        public async Task<OracleSearchItem> SearchAccountAsync(string email, string nationalId = null)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
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
        /// Busca si existe un contacto por email o número de identificación
        /// </summary>
        public async Task<OracleSearchItem> SearchContactAsync(string email, string nationalId = null)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");

                    // Buscar por email
                    var searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/contacts?q=EmailAddress='{Uri.EscapeDataString(email)}'";
                    
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
                        searchUrl = $"{_baseUrl}/crmRestApi/resources/latest/contacts?q=NationalId='{Uri.EscapeDataString(nationalId)}'";
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
                var token = await GetAccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                    var accountJson = JsonConvert.SerializeObject(account);
                    var content = new StringContent(accountJson, Encoding.UTF8, "application/json");

                    var createUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts";
                    var response = await client.PostAsync(createUrl, content);
                    response.EnsureSuccessStatusCode();

                    var responseContent = await response.Content.ReadAsStringAsync();
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
                var token = await GetAccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                    var contactJson = JsonConvert.SerializeObject(contact);
                    var content = new StringContent(contactJson, Encoding.UTF8, "application/json");

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
        /// Asigna un contacto como contacto principal de una cuenta
        /// </summary>
        public async Task<bool> SetPrimaryContactAsync(string accountPartyNumber, string contactPartyNumber)
        {
            try
            {
                var token = await GetAccessTokenAsync();
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Add("Content-Type", "application/json");

                    var updateData = new
                    {
                        PrimaryContact = new OracleContactReference
                        {
                            PartyNumber = contactPartyNumber
                        }
                    };

                    var updateJson = JsonConvert.SerializeObject(updateData);
                    var content = new StringContent(updateJson, Encoding.UTF8, "application/json");

                    var updateUrl = $"{_baseUrl}/crmRestApi/resources/latest/accounts/{accountPartyNumber}";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl)
                    {
                        Content = content
                    };
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();

                    return true;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al asignar contacto principal en Oracle Fusion: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Crea o actualiza una cuenta y asigna un contacto como principal
        /// </summary>
        public async Task<string> CreateOrUpdateAccountWithContactAsync(
            string firstName, 
            string lastName, 
            string middleName, 
            string email, 
            string phone, 
            string nationalId,
            string organizationName = null)
        {
            try
            {
                // Buscar si existe el contacto
                var existingContact = await SearchContactAsync(email, nationalId);
                string contactPartyNumber;

                if (existingContact != null)
                {
                    // Si existe, usar el número de parte existente
                    contactPartyNumber = existingContact.PartyNumber;
                }
                else
                {
                    // Crear nuevo contacto
                    var newContact = new OracleContact
                    {
                        PersonFirstName = firstName,
                        PersonLastName = lastName,
                        PersonMiddleName = middleName ?? "",
                        EmailAddress = email,
                        PrimaryPhoneNumber = phone ?? "",
                        NationalId = nationalId ?? "",
                        NationalIdType = "CUI"
                    };

                    var contactResponse = await CreateContactAsync(newContact);
                    contactPartyNumber = contactResponse.PartyNumber;
                }

                // Buscar si existe la cuenta
                var existingAccount = await SearchAccountAsync(email, nationalId);
                string accountPartyNumber;

                if (existingAccount != null)
                {
                    // Si existe, actualizar el contacto principal
                    accountPartyNumber = existingAccount.PartyNumber;
                    await SetPrimaryContactAsync(accountPartyNumber, contactPartyNumber);
                }
                else
                {
                    // Crear nueva cuenta
                    var accountName = !string.IsNullOrEmpty(organizationName) 
                        ? organizationName 
                        : $"{firstName} {lastName}";

                    var newAccount = new OracleAccount
                    {
                        PartyName = accountName,
                        OrganizationName = accountName,
                        PrimaryContact = new OracleContactReference
                        {
                            PartyNumber = contactPartyNumber
                        }
                    };

                    var accountResponse = await CreateAccountAsync(newAccount);
                    accountPartyNumber = accountResponse.PartyNumber;
                }

                return accountPartyNumber;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error al crear/actualizar cuenta y contacto en Oracle Fusion: {ex.Message}", ex);
            }
        }
    }
}

