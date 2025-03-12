using Avisen.Models;

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
                ImagenUrl = "refrescos.jpg",
                Nombre = "Abarrotes Julia",
                Ubicacion = new Location(19.880369374817423, -103.59103956571073),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "2x1 en Refrescos",
                        Descripcion = "Disfruta de 2x1 en Refrescos Coca-Cola.",
                        Precio = 25, 
                        Vigencia = DateTime.Now.AddDays(7), // Vigencia de 7 días
                        ImagenUrl = "refrescos.jpg"
                    }
                }
            },
            new Negocio
            {
                ImagenUrl = "papel.jpeg",
                Nombre = "Kiosko",
                Ubicacion = new Location(19.879923510547776, -103.5896499459833),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "Descuento del 20% en Papel Higiénico",
                        Descripcion = "Descuento del 20% en todos los papeles higiénicos",
                        Precio = 15, // Descuento aplicado
                        Vigencia = DateTime.Now.AddDays(15), // Vigencia de 15 días
                        ImagenUrl = "papel.jpeg"
                    }
                }
            },

            new Negocio
            {
                ImagenUrl = "cityfit.jpeg",
                Nombre = "CityFit Sayula",
                Ubicacion = new Location(19.88683988956111, -103.60272264082673),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "Descuento del 10% para estudiantes foráneos",
                        Descripcion = "Descuento del 10% en la membresía mensual, solo para estudiantes foraneos",
                        Precio = 220, // Descuento aplicado
                        Vigencia = DateTime.Now.AddDays(15), // Vigencia de 15 días
                        ImagenUrl = "cityfit.jpeg"
                    }
                }
            },

            new Negocio
            {
                ImagenUrl = "bacardi.png",
                Nombre = "Distrito Tequila",
                Ubicacion = new Location(19.883462302819858, -103.59687278247942),
                Promociones = new List<Promocion>
                {
                    new Promocion
                    {
                        Nombre = "Descuento del 15% En botellas Bacardi",
                        Descripcion = "Descuento del 15% en todas las botellas marca 'Bacardi'",
                        Precio = 0, // Descuento aplicado
                        Vigencia = DateTime.Now.AddDays(15), // Vigencia de 15 días
                        ImagenUrl = "bacardi.png"
                    }
                }
            },
        };
    }
}