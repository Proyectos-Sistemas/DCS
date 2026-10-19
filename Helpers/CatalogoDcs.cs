using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Homer_MVC
{
    // Catalogo del form: Departamento (obligatorio) -> Categoria (opcional) -> Tipo de solicitud (opcional).
    // Departamentos del form = 1..n (mismos IDs que el chatbot). Las colas de Fusion (101..108) tambien viven en
    // TBL_DCS_DEPARTAMENTO, pero se reconocen porque las referencia TIPO_SOLICITUD.ID_DEPARTAMENTO.
    public class CatalogoDcs
    {
        private readonly OracleDbContext db;

        public CatalogoDcs(OracleDbContext db)
        {
            this.db = db;
        }

        public List<SelectListItem> Departamentos()
        {
            return db.Departamento
                .Where(d => !db.TipoSolicitud.Any(t => t.ID_DEPARTAMENTO == d.ID_DEPARTAMENTO))
                .OrderBy(d => d.NOMBRE_DEPTO)
                .Select(d => new SelectListItem { Value = d.ID_DEPARTAMENTO.ToString(), Text = d.NOMBRE_DEPTO })
                .ToList();
        }

        public List<SelectListItem> Categorias(int? depId)
        {
            if (depId == null) return new List<SelectListItem>();
            return db.Categoria
                .Where(c => c.ID_DEPARTAMENTO == depId)
                .OrderBy(c => c.NOMBRE_CATEGORIA)
                .Select(c => new SelectListItem { Value = c.ID_CATEGORIA.ToString(), Text = c.NOMBRE_CATEGORIA })
                .ToList();
        }

        public List<SelectListItem> Tipos(int? catId)
        {
            if (catId == null) return new List<SelectListItem>();
            return db.TipoSolicitud
                .Where(t => t.ID_CATEGORIA == catId)
                .OrderBy(t => t.NOMBRE_TIPO_SOLICITUD)
                .Select(t => new SelectListItem { Value = t.ID_TIPO_SOLICITUD.ToString(), Text = t.NOMBRE_TIPO_SOLICITUD })
                .ToList();
        }

        public int? CategoriaDeTipo(int tipoId)
        {
            return db.TipoSolicitud.Where(t => t.ID_TIPO_SOLICITUD == tipoId).Select(t => (int?)t.ID_CATEGORIA).FirstOrDefault();
        }

        public int? DepartamentoDeCategoria(int catId)
        {
            return db.Categoria.Where(c => c.ID_CATEGORIA == catId).Select(c => c.ID_DEPARTAMENTO).FirstOrDefault();
        }

        // Destino del correo: con tipo, la cola de Fusion del tipo; sin tipo, el canal de Fusion del departamento.
        public string CorreoDestino(TipoSolicitudModel tipo, int? depId, bool externo)
        {
            int? destino = tipo != null ? tipo.ID_DEPARTAMENTO : depId;
            var depto = db.Departamento.FirstOrDefault(d => d.ID_DEPARTAMENTO == destino);
            if (depto == null) return null;
            return externo ? depto.CORREO_EXTERNO : depto.CORREO_NOTIFICACION;
        }

        public string NombreDepartamento(int? depId)
        {
            return db.Departamento.Where(d => d.ID_DEPARTAMENTO == depId).Select(d => d.NOMBRE_DEPTO).FirstOrDefault();
        }

        public static int? ParseId(string valor)
        {
            int id;
            return int.TryParse(valor, out id) && id > 0 ? id : (int?)null;
        }
    }
}
