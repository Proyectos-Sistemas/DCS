using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO.Packaging;
using System.Web;

namespace Homer_MVC
{


    public class ModelosListas
    {

    }
    [Table("TBL_DCS_TIPO_SOLICITUD", Schema = "UNIS_INTERFACES")]
    public class TipoSolicitudModel
    {
        [Key]
        public int ID_TIPO_SOLICITUD { get; set; }
        public string NOMBRE_TIPO_SOLICITUD { get; set; }
        public int ID_CATEGORIA { get; set; }
        public int? ID_DEPARTAMENTO { get; set; }   // cola de Fusion (101..108) a la que va el tipo
    }
    [Table("TBL_DCS_CATEGORIA", Schema = "UNIS_INTERFACES")]
    public class CategoriaModel
    {
        [Key]
        public int ID_CATEGORIA { get; set; }
        public string NOMBRE_CATEGORIA { get; set; }
        public int? ID_DEPARTAMENTO { get; set; }   // departamento del form (1..n); el ruteo a Fusion es por tipo
    }
    [Table("TBL_DCS_DEPARTAMENTO", Schema = "UNIS_INTERFACES")]
    public class DepartamentoModel
    {
        [Key]
        public int ID_DEPARTAMENTO { get; set; }
        public string NOMBRE_DEPTO { get; set; }

        public string CORREO_NOTIFICACION { get; set; }
        public string CORREO_EXTERNO { get; set; }
    }

    [Table("TBL_DCS_SOLICITUD", Schema = "UNIS_INTERFACES")]
    public class SolicitudModel
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int? ID_SOLICITUD { get; set; }   // lo devuelve la inserci�n
        public int? ID_TIPO_SOLICITUD { get; set; }   // null = enviada solo con departamento
        public int? ID_DEPARTAMENTO { get; set; }     // departamento elegido en el form (1..n)
        public string EMAIL_SOLICITANTE { get; set; }
        public string DESCRIPCION { get; set; }
        public DateTime? FECHA_CREACION { get; set; }
        public string ESTADO { get; set; }
        public string ORIGEN { get; set; }   // ej. "SCALA" cuando la solicitud viene de ese sistema; null/vacio en el flujo normal
        public string NUMERO_SR { get; set; }   // SR de Fusion vinculado; null mientras no se encuentra
    }

    [Table("TBL_DCS_PARAMETRO", Schema = "UNIS_INTERFACES")]
    public class ParametroModel
    {
        [Key]
        public int ID_PARAMETRO { get; set; }
        public string NOMBRE_PARAMETRO { get; set; }
        public string VALOR { get; set; }
        public DateTime? FECHA_ACTUALIZACION { get; set; }
    }


    public class LogRequest
    {
        public string department { get; set; }
        public string query { get; set; }

    }

}