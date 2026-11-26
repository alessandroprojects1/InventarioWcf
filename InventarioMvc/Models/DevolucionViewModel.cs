using System.Collections.Generic;

namespace InventarioMvc.Models
{
    // Datos que envía el formulario (JSON) al registrar una devolución
    public class DevolucionViewModel
    {
        public int IdVenta { get; set; }
        public string MotivoDevolucion { get; set; }
        public List<DetalleDevolucionViewModel> Detalles { get; set; }
    }

    // Cada producto agregado al detalle de devolución
    public class DetalleDevolucionViewModel
    {
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public bool ProductoDefectuoso { get; set; }
    }
}
