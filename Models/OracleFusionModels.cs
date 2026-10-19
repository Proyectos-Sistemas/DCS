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

        [JsonProperty("OrganizationName")]
        public string OrganizationName { get; set; }

        [JsonProperty("AccountNumber")]
        public string AccountNumber { get; set; }

        // describe la marca como mandatory:false con default ZCA_CUSTOMER, pero Fusion
        // rechaza el create sin ella (HZ-120113) -- hay que enviarla explicita.
        [JsonProperty("Type")]
        public string Type { get; set; } = "ZCA_CUSTOMER";

        [JsonProperty("PrimaryContactPartyNumber")]
        public string PrimaryContactPartyNumber { get; set; }

        // Fusion exige al menos una direccion por Account (ZCA-210069); DCS no la captura.
        [JsonProperty("Address")]
        public List<OracleAddress> Address { get; set; } = new List<OracleAddress> { new OracleAddress() };
    }

    // Child resource accounts/{id}/child/Address, enviado inline en el create
    public class OracleAddress
    {
        [JsonProperty("Address1")]
        public string Address1 { get; set; } = "N/A";

        [JsonProperty("PostalCode")]
        public string PostalCode { get; set; } = "1010";

        // Unico campo inputRequired del child Address; codigo ISO de 2 letras validado por LOV.
        [JsonProperty("Country")]
        public string Country { get; set; } = "GT";

        // La validacion geografica de GT exige County (departamento) + City validos (HZ-120485);
        // valores copiados de cuentas existentes en Fusion.
        [JsonProperty("County")]
        public string County { get; set; } = "Guatemala";

        [JsonProperty("City")]
        public string City { get; set; } = "Ciudad de Guatemala";
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

    // Create de Service Request: solo los campos que llena un SR creado por correo entrante
    // (titulo = asunto, descripcion = cuerpo, canal = correo). Cuenta/Contacto se agregan
    // despues con UpdateServiceRequestPartiesAsync, igual que en prod.
    public class OracleServiceRequest
    {
        [JsonProperty("SrNumber")]
        public string SrNumber { get; set; }

        [JsonProperty("Title")]
        public string Title { get; set; }

        [JsonProperty("SeverityCd")]
        public string SeverityCd { get; set; }

        [JsonProperty("StatusCd")]
        public string StatusCd { get; set; }

        // Max 1000 caracteres segun el describe.
        [JsonProperty("ProblemDescription")]
        public string ProblemDescription { get; set; }

        [JsonProperty("ChannelTypeCd")]
        public string ChannelTypeCd { get; set; } = "ORA_SVC_EMAIL";
    }

    public class OracleServiceRequestResponse
    {
        [JsonProperty("SrNumber")]
        public string SrNumber { get; set; }

        [JsonProperty("SrId")]
        public string SrId { get; set; }

        [JsonProperty("StatusCd")]
        public string StatusCd { get; set; }
    }

    public class OracleServiceRequestSearchResponse
    {
        [JsonProperty("items")]
        public List<OracleServiceRequestResponse> Items { get; set; }
    }
}

