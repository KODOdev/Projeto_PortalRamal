using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace PortalRamal.Models
{
    public class Usuario
    {
        public string Nome { get; set; }
        public string Unidade { get; set; }
        public string SelectedEdificio { get; set; }
        public IEnumerable<SelectListItem> Edificio { get; set; }
        public string SelectedAndar { get; set; }
        public IEnumerable<SelectListItem> Andar { get; set; }
        public string Ramal { get; set; }
    }
}