using Avisen.Models;

namespace Avisen.Services
{
    public interface NegocioService
    {
        Task<List<Negocio>> ObtenerNegociosAsync();
        Task<List<Promocion>> ObtenerPromocionesAsync();
        Task<List<Matriz>> ObtenerMatricesConPromocionesAsync();
    }
}
