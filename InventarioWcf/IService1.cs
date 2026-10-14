using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace InventarioWcf
{
    // NOTA: puede usar el comando "Rename" del menú "Refactorizar" para cambiar el nombre de interfaz "IService1" en el código y en el archivo de configuración a la vez.
    [ServiceContract]
    public interface IService1
    {
        [OperationContract]
        sp_ObtenerProductoPorId_Result ObtenerProductoId(int id);

        [OperationContract]
        string InsertarProducto(string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen);

        [OperationContract]
        string ActualizarProducto(int id, string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen);

        [OperationContract]
        string EliminarProducto(int id);
    }
  
}
