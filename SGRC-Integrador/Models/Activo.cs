using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGRC_Integrador.Models
{
    public class Activo
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; } // HW, SW, DATOS
        public string Ubicacion { get; set; }
        public int ValorC { get; set; }
        public int ValorI { get; set; }
        public int ValorD { get; set; }
        public int ValorTotal => ValorC + ValorI + ValorD;
    }
}