using System.Text;

public class Solicitud
{
    //identidades existentes
    public int IdSolicitud { get; set; }
    public string CNombre { get; set; }
    public decimal NCapitalSolicitado { get; set; }
    public int NCuotas { get; set; }
    public DateTime DFechaRegistro { get; set; }
    public TimeSpan HFechaRegistro { get; set; }
    public string CProducto { get; set; }
    public string CCodCliente { get; set; }
    public string CDocumentoID { get; set; }
    public DateTime? DFechaAprobacion { get; set; }
    public TimeSpan? DHoraAprobacion { get; set; }
    public int IdEstado { get; set; }
    public string CEstado { get; set; }
    public string CAsesor { get; set; }
    public int IdTipoDocumento { get; set; }
    public int IdTipoPersona { get; set; }
    public string CTelefono { get; set; }

  

}