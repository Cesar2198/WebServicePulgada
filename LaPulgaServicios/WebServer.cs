using System;
using System.Data;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;


namespace LaPulgaServicios
{
    public class WebServer
    {
        private readonly HttpListener _listener = new HttpListener();
        private readonly Func<HttpListenerRequest, string> _responderMethod;

        public WebServer(string[] prefixes, Func<HttpListenerRequest, string> method)
        {//activacion de cors
            //HttpListenerContext context = _listener.EndGetContext();
            //HttpListenerRequest request = context.Request;
            //HttpListenerResponse response = context.Response;
            //if (request.HttpMethod == "OPTIONS")
            //{
            //  response.AddHeader("Access-Control-Allow-Headers", "*");
            //}
            //response.AppendHeader("Access-Control-Allow-Origin", "*");


            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            // URI prefixes are required, for example 
            // "http://localhost:8080/index/".
            if (prefixes == null || prefixes.Length == 0)
                throw new ArgumentException("prefixes");

            // A responder method is required
            if (method == null)
                throw new ArgumentException("method");

            foreach (string s in prefixes)
                _listener.Prefixes.Add(s);

            _responderMethod = method;
            _listener.Start();
        }

        public WebServer(Func<HttpListenerRequest, string> method, params string[] prefixes)
            : this(prefixes, method) { }

        public void Run()
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                Console.WriteLine("Servidor Corriendo...");
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");
                                string rstr = _responderMethod(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                                //if (ctx.Request.HttpMethod == "OPTIONS")
                                //{
                                //  ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type, Accept, X-Requested-With");
                                //  ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST");
                                //  ctx.Response.AddHeader("Access-Control-Max-Age", "1728000");
                                //}

                            }
                            catch { } // suppress any exceptions
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch { } // suppress any exceptions
            });
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}
