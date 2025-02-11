using Avisen;

public class NegocioService
{
    public async Task<List<Negocio>> ObtenerNegociosAsync()
    {
        // Simula una llamada asíncrona (esto se reemplazará por una llamada a la API en el futuro)
        await Task.Delay(100); // Simula un retraso de red

        return new List<Negocio>
        {
            new Negocio
            {
                Nombre = "Restaurante La Parrilla",
                Ubicacion = new Location(19.880369374817423, -103.59103956571073),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "2x1 en Tacos",
                        Descripcion = "Disfruta de 2x1 en tacos al pastor todos los martes.",
                        Precio = 0, // Precio especial (2x1)
                        Vigencia = DateTime.Now.AddDays(7) // Vigencia de 7 días
                    },
                    new Promocion
                    {
                        Nombre = "Descuento del 20%",
                        Descripcion = "Descuento del 20% en todo el menú para grupos de 4 o más personas.",
                        Precio = 0, // Descuento aplicado
                        Vigencia = DateTime.Now.AddDays(30) // Vigencia de 30 días
                    }
                }
            },
            new Negocio
            {
                Nombre = "Cafetería Dulce Tentación",
                Ubicacion = new Location(19.879929414954702, -103.589659371905),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "Descuento del 15% en Postres",
                        Descripcion = "Descuento del 15% en todos los postres después de las 6 PM.",
                        Precio = 0, // Descuento aplicado
                        Vigencia = DateTime.Now.AddDays(15) // Vigencia de 15 días
                    }
                }
            },
            // Agrega más negocios y promociones según sea necesario...
        };
    }
}