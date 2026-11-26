using System.Configuration;
using InventarioMvc.ServiceReference1;

namespace InventarioMvc.Servicios
{
    public static class WcfHelper
    {
        public static Service1Client CrearCliente()
        {
            var cliente = new Service1Client("BasicHttpBinding_IService1");

            // Credenciales para la autenticación básica del servicio publicado (HTTPS)
            cliente.ClientCredentials.UserName.UserName = ConfigurationManager.AppSettings["WcfUsuario"];
            cliente.ClientCredentials.UserName.Password = ConfigurationManager.AppSettings["WcfPassword"];

            return cliente;
        }
    }
}