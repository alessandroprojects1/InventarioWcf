using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace InventarioWcf // (O el namespace exacto que tenga tu proyecto)
{
    public class Service1 : IService1
    {
        // Instancia de tu contexto de base de datos / Entity Framework
        // Reemplázalo por el nombre de tu contexto si es diferente (ej. BDInventarioEntities)
        private Model1 db = new Model1(); 

        public string InsertarProducto(Producto producto)
        {
            // Validación de campos requeridos
            if (string.IsNullOrEmpty(producto.Nombre) || string.IsNullOrEmpty(producto.Categoria))
            {
                return "Error: Todos los campos obligatorios deben estar llenos.";
            }

            // Validación de valores positivos para precio y stock
            if (producto.Precio <= 0 || producto.Stock < 0)
            {
                return "Error: El precio debe ser mayor a 0 y el stock no puede ser negativo.";
            }

            try
            {
                db.Producto.Add(producto);
                db.SaveChanges();
                return "Éxito: Producto insertado correctamente.";
            }
            catch (Exception ex)
            {
                return "Error al insertar: " + ex.Message;
            }
        }

        public string ActualizarProducto(Producto producto)
        {
            // Validación de existencia por ID
            var prodExistente = db.Producto.Find(producto.Id);
            if (prodExistente == null)
            {
                return "Error: No se puede actualizar. El producto no existe.";
            }

            // Validación de campos requeridos
            if (string.IsNullOrEmpty(producto.Nombre))
            {
                return "Error: El nombre del producto es obligatorio.";
            }

            // Validación de valores positivos
            if (producto.Precio <= 0 || producto.Stock < 0)
            {
                return "Error: El precio y el stock deben ser valores válidos (positivos).";
            }

            try
            {
                prodExistente.Nombre = producto.Nombre;
                prodExistente.Precio = producto.Precio;
                prodExistente.Stock = producto.Stock;
                prodExistente.Categoria = producto.Categoria;

                db.SaveChanges();
                return "Éxito: Producto actualizado correctamente.";
            }
            catch (Exception ex)
            {
                return "Error al actualizar: " + ex.Message;
            }
        }

        public Producto ObtenerProductoId(int id)
        {
            // SELECT: Buscar producto por ID (devuelve null si no existe)
            var producto = db.Producto.Find(id);
            return producto; 
        }

        public string EliminarProducto(int id)
        {
            // Validación de existencia antes de eliminar
            var producto = db.Producto.Find(id);
            if (producto == null)
            {
                return "Error: No se puede eliminar. El producto con el ID especificado no existe.";
            }

            try
            {
                db.Producto.Remove(producto);
                db.SaveChanges();
                return "Éxito: El producto se eliminó correctamente.";
            }
            catch (Exception ex)
            {
                return "Error al eliminar: " + ex.Message;
            }
        }
    }
}
