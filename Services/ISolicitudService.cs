using System.Collections.Generic;
using System.Threading.Tasks;
using WebPlan.Models;

namespace WebPlan.Services
{
    public interface ISolicitudService
    {
        Task<List<Solicitud>> GetSolicitudesAsync(int nidAgencia, string cEstados);
    }
}