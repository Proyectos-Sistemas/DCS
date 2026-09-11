using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Homer_MVC.Models
{
    // Modelo para Account en Oracle Fusion
    public class OracleAccount
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }

        [JsonProperty("PartyName")]
        public string PartyName { get; set; }

        [JsonProperty("PartyType")]
        public string PartyType { get; set; } = "ORGANIZATION";

        [JsonProperty("OrganizationName")]
        public string OrganizationName { get; set; }

        [JsonProperty("AccountNumber")]
        public string AccountNumber { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; } = "ACTIVE";

        [JsonProperty("PrimaryContact")]
        public OracleContactReference PrimaryContact { get; set; }
    }

    // Modelo para Contact en Oracle Fusion
    public class OracleContact
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }

        [JsonProperty("PersonFirstName")]
        public string PersonFirstName { get; set; }

        [JsonProperty("PersonLastName")]
        public string PersonLastName { get; set; }

        [JsonProperty("PersonMiddleName")]
        public string PersonMiddleName { get; set; }

        [JsonProperty("PersonNameSuffix")]
        public string PersonNameSuffix { get; set; }

        [JsonProperty("PersonTitle")]
        public string PersonTitle { get; set; }

        [JsonProperty("EmailAddress")]
        public string EmailAddress { get; set; }

        [JsonProperty("PrimaryPhoneNumber")]
        public string PrimaryPhoneNumber { get; set; }

        [JsonProperty("PrimaryPhoneType")]
        public string PrimaryPhoneType { get; set; } = "MOBILE";

        [JsonProperty("Status")]
        public string Status { get; set; } = "ACTIVE";

        [JsonProperty("PartyType")]
        public string PartyType { get; set; } = "PERSON";

        [JsonProperty("NationalId")]
        public string NationalId { get; set; }

        [JsonProperty("NationalIdType")]
        public string NationalIdType { get; set; } = "CUI";
    }

    // Referencia a contacto para asignar como principal
    public class OracleContactReference
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }
    }

    // Respuesta de creación de Account
    public class OracleAccountResponse
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }

        [JsonProperty("PartyId")]
        public string PartyId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }
    }

    // Respuesta de creación de Contact
    public class OracleContactResponse
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }

        [JsonProperty("PartyId")]
        public string PartyId { get; set; }

        [JsonProperty("Status")]
        public string Status { get; set; }
    }

    // Respuesta de búsqueda
    public class OracleSearchResponse
    {
        [JsonProperty("items")]
        public List<OracleSearchItem> Items { get; set; }
    }

    public class OracleSearchItem
    {
        [JsonProperty("PartyNumber")]
        public string PartyNumber { get; set; }

        [JsonProperty("PartyId")]
        public string PartyId { get; set; }

        [JsonProperty("PartyName")]
        public string PartyName { get; set; }

        [JsonProperty("EmailAddress")]
        public string EmailAddress { get; set; }
    }

    // Respuesta de autenticación OAuth
    public class OracleOAuthResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }
    }
}

