using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Homer_MVC.Models
{
    public class MailEAModel
    {
        [Required(ErrorMessage = "Campo obligatorio")]
        public string Nombre_Departamento { get; set; }
        public List<SelectListItem> Departamentos { get; set; }

        [Required(ErrorMessage = "Campo obligatorio")]
        public string Nombre_Producto { get; set; }
        public List<SelectListItem> Productos { get; set; }
        public string Nombres { get; set; }
        [Required(ErrorMessage = "Campo obligatorio")]
        public string Apellidos { get; set; }
        public string ApellidoCasada { get; set; }
        public string CUI { get; set; }
        public string IdCampus { get; set; }

        [Required(ErrorMessage = "Campo obligatorio")]
        [DataType(DataType.EmailAddress)]
        [EmailAddress]
        public string correo { get; set; }
        [Required(ErrorMessage = "Campo obligatorio")]
        public string problema { get; set; }
        public bool EstadoCivil { get; set; }

        //[Required(ErrorMessage = "Debe adjuntar un archivo")]
        [FileTypes(".pdf,.png,.jpg,.jpeg,.docx")]
        public HttpPostedFileBase ArchivoAdjunto { get; set; }

        [AttributeUsage(AttributeTargets.Property)]
        public class FileTypesAttribute : ValidationAttribute
        {
            private readonly string[] _types;

            public FileTypesAttribute(string types)
            {
                _types = types.Split(',').Select(t => t.Trim().ToLower()).ToArray();
                ErrorMessage = "El tipo de archivo no es válido. Tipos permitidos: " + types;
            }

            public override bool IsValid(object value)
            {
                if (value == null) return true; // No validar si es null (usa [Required] aparte)

                var file = value as HttpPostedFileBase;
                if (file == null) return false;

                var extension = Path.GetExtension(file.FileName).ToLower();

                return _types.Contains(extension);
            }
        }
    }


}