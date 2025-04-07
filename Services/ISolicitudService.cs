using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using WebPlan.Models;

namespace WebPlan.Services
{
    
    public interface ISolicitudService
    {
        Task<List<Solicitud>> GetSolicitudesAsync(int nidAgencia, string cEstados);
        Task<DataTable> ObtenerPlanPagosPorIdSolicitudAsync(int idSolicitud); // Change dynamic to DataTable
        Task<DataTable> ObtenerDatosSolicitudAsync(int idSolicitud); // Change dynamic to DataTable
    }

}
