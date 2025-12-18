using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SGRC_Integrador.Models
{
    public class Riesgo
    {
        public int Id { get; set; }
        public string ActivoNombre { get; set; }
        public string Amenaza { get; set; }
        public int Probabilidad { get; set; }
        public int Impacto { get; set; }
        public int Nivel => Probabilidad * Impacto;
        public bool Tratado { get; set; }
    }
}