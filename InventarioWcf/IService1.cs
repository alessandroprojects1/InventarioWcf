using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace InventarioWcf
{
    [ServiceContract]
    public interface IService1
    {
        // ===== PA2: mantenimiento de productos =====
        [OperationContract]
        sp_ObtenerProductoPorId_Result ObtenerProductoId(int id);

        [OperationContract]
        string InsertarProducto(string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen);

        [OperationContract]
        string ActualizarProducto(int id, string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen);

        [OperationContract]
        string EliminarProducto(int id);

        // ===== PA3: devoluciones de ventas =====
        [OperationContract]
        DevolucionResponse RegistrarDevolucionVenta(DevolucionRequest request);

        // Usa el SP sp_ConsultarPendientesDevolucion (apoyo para las pruebas)
        [OperationContract]
        List<sp_ConsultarPendientesDevolucion_Result> ConsultarPendientesDevolucion(int idVenta);
    }

    // ---------------------------------------------------------------------
    // Contratos de datos de la devolución
    // ---------------------------------------------------------------------

    [DataContract]
    public class DetalleDevolucionRequest
    {
        [DataMember]
        public int IdProducto { get; set; }

        [DataMember]
        public int Cantidad { get; set; }

        // false (valor por defecto) = el producto vuelve al inventario
        // true = producto dañado, se registra la devolución pero NO suma stock
        [DataMember]
        public bool ProductoDefectuoso { get; set; }
    }

    [DataContract]
    public class DevolucionRequest
    {
        [DataMember]
        public int IdVenta { get; set; }

        [DataMember]
        public string MotivoDevolucion { get; set; }

        [DataMember]
        public List<DetalleDevolucionRequest> Detalles { get; set; }
    }

    [DataContract]
    public class DevolucionResponse
    {
        [DataMember]
        public bool Exito { get; set; }

        [DataMember]
        public int IdDevolucion { get; set; }

        [DataMember]
        public string TipoDevolucion { get; set; }

        [DataMember]
        public decimal MontoTotalDevuelto { get; set; }

        [DataMember]
        public string Mensaje { get; set; }
    }
}
