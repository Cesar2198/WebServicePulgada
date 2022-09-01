using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaPulgaServicios
{
   public class detalleSolicitud
    {
        public string codinv { set; get; }

        public string descrip { set; get; }

        public Double precio { set; get; }

        public string bodega { set; get; }

        public int existencias { set; get; }


        public int cantidad { set; get; }

    }

    public class Elementos
    {
        public List<detalleSolicitud> data { set; get; }
    }
}
