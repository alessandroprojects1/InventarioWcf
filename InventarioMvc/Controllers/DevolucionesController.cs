using System;
using System.Linq;
using System.ServiceModel;
using System.Web.Mvc;
using InventarioMvc.Models;
using InventarioMvc.ServiceReference1;
using InventarioMvc.Servicios;

namespace InventarioMvc.Controllers
{
    public class DevolucionesController : Controller
    {
        // GET: Devoluciones  -> listado (consume ListarDevoluciones)
        public ActionResult Index()
        {
            var cliente = WcfHelper.CrearCliente();
            try
            {
                DevolucionListado[] devoluciones = cliente.ListarDevoluciones();
                return View(devoluciones);
            }
            catch (Exception ex)
            {
                ViewBag.Error = "No se pudo obtener el listado de devoluciones: " + ex.Message;
                return View(new DevolucionListado[0]);
            }
            finally
            {
                CerrarCliente(cliente);
            }
        }

        // GET: Devoluciones/Detalle/5  -> detalle (consume VerDevolucionDetalle)
        public ActionResult Detalle(int id)
        {
            var cliente = WcfHelper.CrearCliente();
            try
            {
                DevolucionDetalleItem[] detalle = cliente.VerDevolucionDetalle(id);
                ViewBag.IdDevolucion = id;
                return View(detalle);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "No se pudo obtener el detalle de la devolución " + id + ": " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                CerrarCliente(cliente);
            }
        }

        // GET: Devoluciones/Create  -> formulario de registro
        public ActionResult Create()
        {
            return View();
        }

        // GET: Devoluciones/ProductosVenta?idVenta=1
        // Devuelve en JSON los productos de la venta con lo vendido, devuelto y pendiente
        // (consume ConsultarPendientesDevolucion, que usa el SP de la PA3)
        public JsonResult ProductosVenta(int idVenta)
        {
            var cliente = WcfHelper.CrearCliente();
            try
            {
                var productos = cliente.ConsultarPendientesDevolucion(idVenta);
                if (productos == null || productos.Length == 0)
                    return Json(new { exito = false, mensaje = "La venta " + idVenta + " no existe o no tiene productos." },
                                JsonRequestBehavior.AllowGet);

                var lista = productos.Select(p => new
                {
                    idProducto = p.IdProducto,
                    nombre = p.NombreProducto,
                    vendida = p.CantidadVendida ?? 0,
                    devuelta = p.CantidadDevuelta,
                    pendiente = p.CantidadPendiente ?? 0,
                    precio = p.PrecioVenta ?? 0
                });
                return Json(new { exito = true, productos = lista }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = "Error al consumir el servicio WCF: " + ex.Message },
                            JsonRequestBehavior.AllowGet);
            }
            finally
            {
                CerrarCliente(cliente);
            }
        }

        // POST: Devoluciones/Registrar  -> recibe el detalle y consume RegistrarDevolucionVenta
        [HttpPost]
        public JsonResult Registrar(DevolucionViewModel model)
        {
            // Validaciones en el cliente MVC (datos incorrectos)
            if (model == null || model.IdVenta <= 0)
                return Json(new { exito = false, mensaje = "Ingrese un Id de venta válido." });
            if (string.IsNullOrWhiteSpace(model.MotivoDevolucion))
                return Json(new { exito = false, mensaje = "Ingrese el motivo de la devolución." });
            if (model.Detalles == null || model.Detalles.Count == 0)
                return Json(new { exito = false, mensaje = "Agregue al menos un producto al detalle de la devolución." });

            // ViewModel -> DevolucionRequest del servicio WCF
            var request = new DevolucionRequest
            {
                IdVenta = model.IdVenta,
                MotivoDevolucion = model.MotivoDevolucion.Trim(),
                Detalles = model.Detalles.Select(d => new DetalleDevolucionRequest
                {
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    ProductoDefectuoso = d.ProductoDefectuoso
                }).ToArray()
            };

            var cliente = WcfHelper.CrearCliente();
            try
            {
                DevolucionResponse respuesta = cliente.RegistrarDevolucionVenta(request);

                if (respuesta.Exito)
                    TempData["Exito"] = respuesta.Mensaje;   // se muestra en el listado

                return Json(new { exito = respuesta.Exito, mensaje = respuesta.Mensaje, id = respuesta.IdDevolucion });
            }
            catch (Exception ex)
            {
                return Json(new { exito = false, mensaje = "Error al consumir el servicio WCF: " + ex.Message });
            }
            finally
            {
                CerrarCliente(cliente);
            }
        }

        // Cierra el cliente WCF; si quedó en error (Faulted) hay que abortarlo
        private static void CerrarCliente(Service1Client cliente)
        {
            if (cliente.State == CommunicationState.Faulted)
                cliente.Abort();
            else
                cliente.Close();
        }
    }
}
