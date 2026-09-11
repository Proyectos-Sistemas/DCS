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
    [Table("TBL_PRODUCTO_DCS", Schema = "UNIS_INTERFACES")]
    public class ProductoModel
    {
        [Key]
        public int ID_PRODUCTO { get; set; }
        public string NOMBRE_PRODUCTO { get; set; }
        public string COLA { get; set; }
        public Nullable<int> ID_CATEGORIA { get; set; }
    }
    [Table("TBL_CATEGORIA_DCS", Schema = "UNIS_INTERFACES")]
    public class CategoriaModel
    {
        [Key]
        public int ID_CATEGORIA { get; set; }
        public string NOMBRE_CATEGORIA { get; set; }
    }
    [Table("TBL_DEPARTAMENTO_DCS", Schema = "UNIS_INTERFACES")]
    public class DepartamentoModel
    {
        [Key]
        public int ID_DEPARTAMENTO { get; set; }
        public string NOMBRE_DEPTO { get; set; }

        public string CORREO_NOTIFICACION { get; set; }
        public string CORREO_EXTERNO { get; set; }
    }

    [Table("TBL_SOLICITUD_DCS", Schema = "UNIS_INTERFACES")]
    public class SolicitudModel
    {
        [Key]
        public int? ID_SOLICITUD { get; set; }   // lo devuelve la inserción
        public string EMAIL { get; set; }
        public string CUI { get; set; }
        public string CODIGO_CAMPUS { get; set; }
        public string NOMBRES { get; set; }
        public string APELLIDOS { get; set; }
        public string APELLIDO_CASADA { get; set; }
        public string DEPARTAMENTO { get; set; }
        public string SOLICITUD { get; set; } // “Solicitud” del formulario
        public string DESCRIPCION { get; set; }
        public string ARCHIVO_NOMBRE { get; set; }
        public string ARCHIVO_RUTA { get; set; }
        public DateTime? CREATED_AT { get; set; }
        public string CREATED_IP { get; set; }
    }

    [Table("TBL_PARAMETRO_DCS", Schema = "UNIS_INTERFACES")]
    public class ParametroModel
    {
        [Key]
        public string PARAMETRO { get; set; }
        public string VALOR { get; set; }
    }


    public class LogRequest
    {
        public string department { get; set; }
        public string query { get; set; }

    }

}