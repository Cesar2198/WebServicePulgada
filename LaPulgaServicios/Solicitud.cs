using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaPulgaServicios
{
    class Solicitud
    {
        public Int32 idSol { get; set; }
        public Int32 idBodega { get; set; }
        public String NombreUsuario { get; set; }
        public Solicitud() { }
    }
}
