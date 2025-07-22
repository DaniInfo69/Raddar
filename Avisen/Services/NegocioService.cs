using Avisen.Models;

namespace Avisen.Services
{
    public interface NegocioService
    {
        Task<List<Negocio>> ObtenerNegociosAsync();
        Task<List<Promocion>> ObtenerPromocionesAsync();
        Task<List<Matriz>> ObtenerMatricesConPromocionesAsync();

        Task<List<Negocio>> ObtenerNegociosConPromocionesAsync();
        Task<List<Matriz>> ObtenerPromocionesEnRangoAsync(double lat, double lng, double rango);
    }
}
