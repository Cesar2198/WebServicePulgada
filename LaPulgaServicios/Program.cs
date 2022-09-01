using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using MySql.Data.MySqlClient;
using System.IO;
using System.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Web.Script.Serialization;

namespace LaPulgaServicios
{
    class Program
    {
        public static void Main(string[] args)
        {
            if (File.Exists("PuertoActual.inf") == false)
            {
                using (StreamWriter _rw = File.CreateText("PuertoActual.inf"))
                {
                    _rw.WriteLine("6001");
                    _rw.WriteLine("localhost");
                }
            }
            String _myport = "";
            String _servidor = "";
            using (StreamReader _rd = File.OpenText("PuertoActual.inf"))
            {
                _myport = _rd.ReadLine();
                _servidor = _rd.ReadLine();

            }
            String _servicio = String.Format("http://{0}:{1}/servicio/", _servidor, _myport);
            WebServer ws = new WebServer(SendResponse, _servicio);
            ws.Run();
            Console.WriteLine("Servicio de Quickfact Version La Pulgada- Presione q para salir - " +
                            _servicio);
            Int32 _seguircorriendo = 1;
            

            while (_seguircorriendo == 1)
            {
                ConsoleKeyInfo _key = Console.ReadKey();
                if (_key.Key == ConsoleKey.Q)
                {
                    ws.Stop();
                    _seguircorriendo = 0;
                }
            }
        }
        public static String MensajeError(String pMensaje)
        {
            return string.Format("Error:", pMensaje);
        }
        public static MySqlConnection Conexion()
        {
            String _path = System.AppDomain.CurrentDomain.BaseDirectory;
            StreamReader _re = File.OpenText(_path + "conexion.inf");
            String _conexi = _re.ReadLine();
            _re.Close();
            MySqlConnection _conexion = new MySqlConnection();
            //OdbcConnection _conexion = new OdbcConnection("DSN=ContpreRemoto;");
            _conexion.ConnectionString = _conexi;

            return _conexion;
        }
        public static String SqlFecha(DateTime pFecha)
        {
            return String.Format("{0}-{1}-{2}", pFecha.Year, pFecha.Month.ToString().PadLeft(2, '0'),
                pFecha.Day.ToString().PadLeft(2, '0'));
        }
        public static String SqlFecha(String pFecha)
        {
            //2021-12-31
            //0123456789
            return String.Format("{0}-{1}-{2}", pFecha.Substring(0, 4), pFecha.Substring(5, 2),
                pFecha.Substring(8, 2));
        }
        public static MySqlParameter Parametro(String pnombre, DbType pTipo, object pValor)
        {
            MySqlParameter _param = new MySqlParameter(pnombre, pValor);
            _param.DbType = pTipo;
            return _param;
        }
        public static IDbCommand Comando()
        {
            MySqlCommand _commando = new MySqlCommand();
            //OdbcCommand _commando = new OdbcCommand();
            return _commando;
        }
        public static MySqlCommand Comando(MySqlConnection pConn)
        {
            MySqlCommand _commando = new MySqlCommand();
            _commando.Connection = pConn;
            //OdbcCommand _commando = new OdbcCommand();
            return _commando;
        }
        public static Boolean EjecutarQuery(String pquery)
        {
            try
            {
                MySqlConnection _conn = Conexion();
                MySqlCommand _comm = Comando(_conn);

                _conn.Open();

                _comm.CommandText = pquery;
                _comm.ExecuteNonQuery();

                _conn.Close();
                return true;

            }
            catch
            {
                return false;
            }



        }


        public static String ObtenerQuery(String pquery)
        {
            MySqlConnection _conn = Conexion();
            MySqlCommand _comm = Comando(_conn);
            try
            {
                _conn.Open();
                DataTable _t = new DataTable();
                _comm.CommandText = pquery;
                IDataReader _reader = _comm.ExecuteReader();
                _t.Load(_reader);
                _conn.Close();

                return JsonConvert.SerializeObject(_t);
            }
            catch (Exception ex)
            {
                return "Error:" + ex.Message;
            }

        }
        public static DataTable ObtenerQueryLocal(String pquery)
        {
            MySqlConnection _conn = Conexion();
            MySqlCommand _comm = Comando(_conn);
            try
            {
                _conn.Open();
                DataTable _t = new DataTable();
                _comm.CommandText = pquery;
                IDataReader _reader = _comm.ExecuteReader();
                _t.Load(_reader);
                _conn.Close();

                return _t;
            }
            catch (Exception ex)
            {
                return new DataTable();
            }

        }

        public static string SendResponse(HttpListenerRequest request)
        {
            var query = request.QueryString;

            Int32 variableTotal = query.Count;
            if (variableTotal > 0)
            {
                try
                {
                    // ?verificador=SyscomeAuth$280257&empresa=1&clave254
                    String _verifier = query["verif"];
                    if (_verifier != "scm02")
                    {
                        return MensajeError("ACCESO DENEGADO");
                    }
                    String _q = query["q"];

                    if (_q == "t")
                    {
                        return DateTime.Now.ToString();
                    }
                    if (_q == "validarusuario")
                    {
                        DataTable _t = ObtenerQueryLocal(
                            String.Format("select ifnull(id, 0) as id, idbodega, nombre from usuarios where "+
                            " nombre = '{0}' and clave = sha1('{1}');", 
                            query["usr1"], query["pwd1"]));
                        DatosdeSesion _datos = new DatosdeSesion();
                        _datos.UserId = 0;
                        _datos.NombreUsuario = "Clave Erronea";
                        if (_t.Rows.Count > 0)
                        {
                            _datos.UserId = Convert.ToInt32(_t.Rows[0][0]);
                            _datos.Sucursal = Convert.ToInt32(_t.Rows[0][1]);
                            _datos.NombreUsuario = Convert.ToString(_t.Rows[0][2]);
                        }
                        return JsonConvert.SerializeObject(_datos);
                    }
                    else if (_q == "obtenersucursales")
                    {
                        return ObtenerQuery("select id, descrip from bodega");
                    }
                    else if (_q == "obtenerproductos")
                    {
                        return ObtenerQuery(
                                String.Format("select inv.codinv, inv.descrip, inv.prec1 as precio,b.descrip, binv.saldou as existencias " +
                                    "from maeinv inv, bodega_inv binv, bodega b " +
                                    "where inv.codinv = binv.CODINV and " +
                                    "b.id = binv.BODEGA and b.descrip = '{0} 'and inv.descrip like '%{1}%' " +
                                    "group by inv.codinv;", query["bodega"], query["nombre"]));

                    }
                    else if (_q == "generarsolicitud")
                    {
                       bool respuesta = EjecutarQuery(
                             String.Format("insert into solic_prod (idbodega,usuario, codinv, cantidad, fecha, codigo, nombre)" +
                            "values({0}, '{1}', null , null, '{2}', '{3}', '{4}');",
                           query["bodega"], query["usuario"], query["fecha"], query["codigo"], query["nombre"]));
                        // verificaremos si lo primero se ingreso
                        if (respuesta)
                        { /// buscaremos la ultima solicitud ingresada
                            DataTable _t = ObtenerQueryLocal(
                             String.Format("SELECT * FROM solic_prod s " +
                             " order by id desc;"));
                            /// Obtenemos el ultimo id de la solicitud esta para motivo de ingresar y hacer la transaccion
                            Solicitud _sol = new Solicitud();
                            /// recorremos la datatable
                            if (_t.Rows.Count > 0)
                            {
                                _sol.idSol = Convert.ToInt32(_t.Rows[0][0]);
                                _sol.idBodega = Convert.ToInt32(_t.Rows[0][1]);
                                _sol.NombreUsuario = Convert.ToString(_t.Rows[0][2]);
                            }

                            var lista = query["list"];
                            ///convertimos el objeto a un jsonData, lo decodificamos y lo asignamos a un array de detalle de solicitud
                            JavaScriptSerializer js = new JavaScriptSerializer();
                            detalleSolicitud[] d_ = js.Deserialize<detalleSolicitud[]>(lista);
                            List<detalleSolicitud> _lista = JsonConvert.DeserializeObject<List<detalleSolicitud>>(lista);
                            foreach(var _item in _lista)
                            {
                                var total = _item.cantidad * _item.precio;
                                EjecutarQuery(
                                  String.Format("insert into solic_prod_det(idhead, codinv, descrip, cantidad, pmedi,total)" +
                                 "values({0},'{1}','{2}','{3}', {4}, {5});",
                                 _sol.idSol, _item.codinv, _item.descrip, _item.cantidad, _item.precio, total));
                            }
 


                            /// enviamos el mensaje que todo fue satisfactorio...
                            return "Información ingresada con éxito.";
                        }
                        else
                        {
                            /// mostraremos un mensaje instantaneo de error
                            return "Ha ocurrido un error en la inserción.";
                        }
                    }
                    else if (_q == "obtenerclientes")
                    {
                        return ObtenerQuery(
                               String.Format("SELECT * FROM cliente c where nombre like '%{0}%';", query["nombre"]));
                    } else if(_q == "obtenersucursales")
                    {
                        String.Format("SELECT * FROM bodega ;");
                    }
                    else if(_q == "obtenerexistencias")
                    {
                        return ObtenerQuery(
                                    String.Format("select inv.codinv, inv.descrip, inv.prec1 as precio, b.descrip as bodega, binv.saldou as existencias" +
                                        " from maeinv inv, bodega_inv binv, bodega b" +
                                        " where inv.codinv = binv.CODINV and " +
                                        " b.id = binv.BODEGA and inv.descrip = '{0}' group by b.descrip", query["nombre"]));
       
                    }
                    else if (_q == "obtenerexistenciasporbodega")
                    {
                        return ObtenerQuery(
                             String.Format("select m.codinv, m.descrip, m.prec1 as precio, b.descrip as bodega, i.saldou as existencias " +
                                 " from maeinv m inner " +
                                 "join bodega_inv i " +
                                 " on m.codinv = i.codinv " +
                                 " inner join bodega b on i.bodega = b.id " +
                                 "where m.descrip like '%{0}%'"+
                                 "and b.descrip like '%{1}%'"
                                 , query["nombre"], query["bodega"]));

                    }
                    return MensajeError("Acceso Denegado");

                    // _q son las opciones que puede utilizar
                    // A -> 1. Verificacion de Usuario
                    // B -> 2. Obtener Planificacion Actual
                    // C -> 3. Actualizar Resultado
                    // D -> 4. Obtener Datos de Asociado

                }
                catch (Exception lcex)
                {
                    return MensajeError(lcex.Message);
                }



            }
            else
            {
                return MensajeError("ACCESO DENEGADO");
            }
        }

    }
}
