using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Transactions;   // Requiere la referencia System.Transactions (ver paso 4)

namespace InventarioWcf // (O el namespace exacto que tenga tu proyecto)
{
    public class Service1 : IService1
    {
        // =================================================================
        // PA2: MANTENIMIENTO DE PRODUCTOS (con los SP de la PA2)
        // =================================================================

        public sp_ObtenerProductoPorId_Result ObtenerProductoId(int id)
        {
            using (var db = new InventarioDBEntities())
            {
                return db.sp_ObtenerProductoPorId(id).FirstOrDefault();
            }
        }

        public string InsertarProducto(string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "Error: El nombre del producto es obligatorio.";
            if (precio <= 0)
                return "Error: El precio debe ser mayor a 0.";
            if (stock < 0)
                return "Error: El stock no puede ser negativo.";

            try
            {
                using (var db = new InventarioDBEntities())
                {
                    if (!db.Categorias.Any(c => c.id == categoria_id))
                        return "Error: La categoría indicada no existe.";

                    db.sp_InsertarProducto(nombre.Trim(), descripcion, precio, stock, categoria_id, url_imagen);
                    return "Éxito: Producto insertado correctamente.";
                }
            }
            catch (Exception ex)
            {
                return "Error al insertar: " + MensajeInterno(ex);
            }
        }

        public string ActualizarProducto(int id, string nombre, string descripcion, decimal precio, int stock, int categoria_id, string url_imagen)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                return "Error: El nombre del producto es obligatorio.";
            if (precio <= 0 || stock < 0)
                return "Error: El precio debe ser mayor a 0 y el stock no puede ser negativo.";

            try
            {
                using (var db = new InventarioDBEntities())
                {
                    if (!db.Productos.Any(p => p.id == id))
                        return "Error: No se puede actualizar. El producto no existe.";
                    if (!db.Categorias.Any(c => c.id == categoria_id))
                        return "Error: La categoría indicada no existe.";

                    db.sp_ActualizarProducto(id, nombre.Trim(), descripcion, precio, stock, categoria_id, url_imagen);
                    return "Éxito: Producto actualizado correctamente.";
                }
            }
            catch (Exception ex)
            {
                return "Error al actualizar: " + MensajeInterno(ex);
            }
        }

        public string EliminarProducto(int id)
        {
            try
            {
                using (var db = new InventarioDBEntities())
                {
                    if (!db.Productos.Any(p => p.id == id))
                        return "Error: No se puede eliminar. El producto con el ID especificado no existe.";

                    db.sp_EliminarProducto(id);
                    return "Éxito: El producto se eliminó correctamente.";
                }
            }
            catch (Exception ex)
            {
                return "Error al eliminar: " + MensajeInterno(ex);
            }
        }

        // =================================================================
        // PA3: REGISTRAR DEVOLUCIÓN DE VENTA (transacción)
        // =================================================================

        public DevolucionResponse RegistrarDevolucionVenta(DevolucionRequest request)
        {
            try
            {
                // ---------- 1. Validaciones de los datos recibidos ----------
                if (request == null)
                    throw new ArgumentException("La solicitud de devolución es obligatoria.");

                if (request.IdVenta <= 0)
                    throw new ArgumentException("El Id de la venta debe ser mayor que cero.");

                if (string.IsNullOrWhiteSpace(request.MotivoDevolucion))
                    throw new ArgumentException("El motivo de la devolución es obligatorio.");

                if (request.MotivoDevolucion.Trim().Length > 250)
                    throw new ArgumentException("El motivo de la devolución no puede superar los 250 caracteres.");

                if (request.Detalles == null || request.Detalles.Count == 0)
                    throw new ArgumentException("Debe indicar al menos un producto a devolver.");

                foreach (var item in request.Detalles)
                {
                    if (item == null)
                        throw new ArgumentException("El detalle de la devolución contiene un elemento vacío.");
                    if (item.IdProducto <= 0)
                        throw new ArgumentException("El Id del producto debe ser mayor que cero.");
                    if (item.Cantidad <= 0)
                        throw new ArgumentException($"La cantidad a devolver del producto {item.IdProducto} debe ser mayor que cero.");
                }

                var repetido = request.Detalles
                    .GroupBy(d => d.IdProducto)
                    .FirstOrDefault(g => g.Count() > 1);
                if (repetido != null)
                    throw new ArgumentException($"El producto {repetido.Key} está repetido en la solicitud. Envíelo una sola vez con la cantidad total.");

                // ---------- 2. Transacción ----------
                // TransactionScope usa aislamiento Serializable: mientras dura la
                // transacción, nadie más puede registrar otra devolución que use
                // las mismas cantidades. Si ocurre cualquier excepción antes de
                // scope.Complete(), al salir del using se hace ROLLBACK de todo.
                using (var scope = new TransactionScope())
                using (var db = new InventarioDBEntities())
                {
                    // 2.1 La venta debe existir
                    var venta = db.Ventas.FirstOrDefault(v => v.Id == request.IdVenta);
                    if (venta == null)
                        throw new ArgumentException($"La venta {request.IdVenta} no existe.");

                    // 2.2 Cantidades vendidas por producto en esa venta
                    var vendidos = db.DetalleVenta
                        .Where(dv => dv.IdVenta == request.IdVenta)
                        .GroupBy(dv => dv.IdProducto)
                        .Select(g => new
                        {
                            IdProducto = g.Key,
                            Cantidad = g.Sum(x => x.Cantidad),
                            Importe = g.Sum(x => x.Cantidad * x.Precio)
                        })
                        .ToList();

                    // 2.3 Cantidades ya devueltas antes, por producto, de esa venta
                    var devueltos = (from dd in db.DetalleDevolucion
                                     join de in db.Devolucion on dd.IdDevolucion equals de.Id
                                     where de.IdVenta == request.IdVenta
                                     group dd by dd.IdProducto into g
                                     select new { IdProducto = g.Key, Cantidad = g.Sum(x => x.Cantidad) })
                                    .ToList()
                                    .ToDictionary(x => x.IdProducto, x => x.Cantidad);

                    // 2.4 Validar cada producto contra lo pendiente de devolución
                    var detalles = new List<DetalleDevolucion>();
                    decimal montoTotal = 0;

                    foreach (var item in request.Detalles)
                    {
                        var vendido = vendidos.FirstOrDefault(v => v.IdProducto == item.IdProducto);
                        if (vendido == null)
                            throw new ArgumentException($"El producto {item.IdProducto} no pertenece a la venta {request.IdVenta}.");

                        int yaDevuelto = devueltos.ContainsKey(item.IdProducto) ? devueltos[item.IdProducto] : 0;
                        int pendiente = vendido.Cantidad - yaDevuelto;

                        if (pendiente <= 0)
                            throw new ArgumentException($"El producto {item.IdProducto} ya fue devuelto en su totalidad en esta venta.");

                        if (item.Cantidad > pendiente)
                            throw new ArgumentException(
                                $"La cantidad a devolver del producto {item.IdProducto} ({item.Cantidad}) supera la pendiente de devolución " +
                                $"({pendiente}). Vendidas: {vendido.Cantidad}, ya devueltas: {yaDevuelto}.");

                        // Precio al que se vendió (no el precio actual del producto)
                        decimal precioVenta = Math.Round(vendido.Importe / vendido.Cantidad, 2);
                        montoTotal += precioVenta * item.Cantidad;

                        detalles.Add(new DetalleDevolucion
                        {
                            IdProducto = item.IdProducto,
                            Cantidad = item.Cantidad,
                            PrecioVenta = precioVenta,
                            ReingresaInventario = !item.ProductoDefectuoso
                        });
                    }

                    // 2.5 Tipo: COMPLETA si con esta devolución ya no queda nada
                    //     pendiente de la venta; si no, PARCIAL.
                    int totalPendienteAntes = vendidos.Sum(v => v.Cantidad) - devueltos.Values.Sum();
                    int totalADevolver = request.Detalles.Sum(d => d.Cantidad);
                    string tipo = totalADevolver == totalPendienteAntes ? "COMPLETA" : "PARCIAL";

                    // 2.6 Registrar la cabecera
                    var devolucion = new Devolucion
                    {
                        IdVenta = request.IdVenta,
                        Fecha = DateTime.Now,
                        MotivoDevolucion = request.MotivoDevolucion.Trim(),
                        TipoDevolucion = tipo,
                        MontoTotalDevuelto = montoTotal
                    };
                    db.Devolucion.Add(devolucion);
                    db.SaveChanges();   // genera devolucion.Id

                    // 2.7 Registrar el detalle y actualizar el stock
                    foreach (var det in detalles)
                    {
                        det.IdDevolucion = devolucion.Id;
                        db.DetalleDevolucion.Add(det);

                        if (det.ReingresaInventario)
                        {
                            var producto = db.Productos.FirstOrDefault(p => p.id == det.IdProducto);
                            if (producto == null)
                                throw new InvalidOperationException($"El producto {det.IdProducto} ya no existe en el inventario.");

                            producto.stock += det.Cantidad;
                        }
                    }
                    db.SaveChanges();

                    // 2.8 Confirmar (COMMIT)
                    scope.Complete();

                    return new DevolucionResponse
                    {
                        Exito = true,
                        IdDevolucion = devolucion.Id,
                        TipoDevolucion = tipo,
                        MontoTotalDevuelto = montoTotal,
                        Mensaje = $"Éxito: Devolución {tipo} N° {devolucion.Id} registrada para la venta {request.IdVenta}. " +
                                  $"Monto devuelto: {montoTotal:0.00}."
                    };
                }
            }
            catch (ArgumentException ex)
            {
                // Errores de validación (no se guardó nada)
                return Error("Error de validación: " + ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Error("Error: " + ex.Message + " Se revirtieron los cambios.");
            }
            catch (DbUpdateException ex)
            {
                // Error al guardar en SQL Server (FK, CHECK, etc.) -> rollback
                return Error("Error al guardar en la base de datos, se revirtieron los cambios: " + MensajeInterno(ex));
            }
            catch (TransactionAbortedException ex)
            {
                return Error("La transacción fue cancelada y se revirtieron los cambios: " + MensajeInterno(ex));
            }
            catch (Exception ex)
            {
                return Error("Error inesperado al registrar la devolución, se revirtieron los cambios: " + MensajeInterno(ex));
            }
        }

        public List<sp_ConsultarPendientesDevolucion_Result> ConsultarPendientesDevolucion(int idVenta)
        {
            using (var db = new InventarioDBEntities())
            {
                return db.sp_ConsultarPendientesDevolucion(idVenta).ToList();
            }
        }

        // =================================================================
        // Métodos de apoyo
        // =================================================================

        private static DevolucionResponse Error(string mensaje)
        {
            return new DevolucionResponse
            {
                Exito = false,
                IdDevolucion = 0,
                TipoDevolucion = null,
                MontoTotalDevuelto = 0,
                Mensaje = mensaje
            };
        }

        // Devuelve el mensaje más interno (el real de SQL Server)
        private static string MensajeInterno(Exception ex)
        {
            while (ex.InnerException != null) ex = ex.InnerException;
            return ex.Message;
        }
    }
}