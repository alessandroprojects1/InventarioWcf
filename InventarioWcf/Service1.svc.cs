using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace InventarioWcf
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de clase "Service1" en el código, en svc y en el archivo de configuración.
    // NOTE: para iniciar el Cliente de prueba WCF para probar este servicio, seleccione Service1.svc o Service1.svc.cs en el Explorador de soluciones e inicie la depuración.
    public class Service1 : IService1
    {


        public sp_ObtenerProductoPorId_Result ObtenerProductoId(int id) {

            using (var db = new InventarioDBEntities()) {


                var productoSP = db.sp_ObtenerProductoPorId(id).FirstOrDefault();
                
                return productoSP;
                
            }
        }

        public string InsertarProducto(string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen) {

            using (var db = new InventarioDBEntities()) {

                var producto = db.sp_InsertarProducto(nombre,descripcion,precio, stock, categoria_id, url_imagen);

                if (producto > 0) {
                    return "Producto agregado con exito";
                }
                else {

                    return "Error en el registro del producto";
                }
            }
        }


        public string ActualizarProducto(int id, string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen) {

            using (var db = new InventarioDBEntities()) {

                var producto = db.sp_ActualizarProducto(id, nombre, descripcion, precio, stock, categoria_id, url_imagen);

                if (producto > 0)
                {
                    return "Se actualizo el producto con éxito";
                }
                else {
                    return "No se pudo actualizar el producto";
                }
            }

        }


        public string EliminarProducto(int id) {

            using (var db = new InventarioDBEntities()) {


                var producto = db.sp_EliminarProducto(id);

                if (producto > 0) {
                    return "El producto se elimino correctamente";
                }
                else {
                    return "No se pudo eliminar el producto";
                }
            }
        }

      
    }
}
