using Microsoft.AspNetCore.Mvc;
using WebPlan.Models;
using WebPlan.Services; // Asegúrate de que este espacio de nombres y el ensamblado estén referenciados correctamente
using WebPlan.Models;
using WebPlan.Services; // Asegúrate de que este espacio de nombres y el ensamblado estén referenciados correctamente


using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Data;
using System.Runtime.ConstrainedExecution;
using System.Reflection.Metadata.Ecma335;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace WebPlan.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ISolicitudService _solicitudService;

        // Un único constructor que recibe todas las dependencias
        public HomeController(
            ILogger<HomeController> logger,
            ISolicitudService solicitudService)
        {
            _logger = logger;
            _solicitudService = solicitudService;
        }

        public IActionResult Index() => View();

        public IActionResult Privacy() => View();

        public IActionResult Solicitud() => View();

        public IActionResult TipoSeguro() => View();

        public IActionResult PlanPagos() => View();

        [HttpPost]
        public async Task<IActionResult> CargarSolicitudes()
        {
            try
            {
                var solicitudes = await _solicitudService.GetSolicitudesAsync(4, "2");
                return PartialView("_SolicitudesPartial", solicitudes);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en CargarSolicitudes");
                return StatusCode(500, new { error = ex.Message });
            }
        }
        public async Task<IActionResult> ObtenerPlanPagos(int idSolicitud)
        {
            // Opción 1: Usando DataTable (directamente desde el SP)
            var planPagos = await _solicitudService.ObtenerPlanPagosPorIdSolicitudAsync(idSolicitud);
            var SolicitudPlan = await _solicitudService.ObtenerDatosSolicitudAsync(idSolicitud);

            ViewBag.PlanPagos = planPagos; // Pasa el DataTable a la vista
            ViewBag.SolicitudPlan = SolicitudPlan; // Pasa el DataTable a la vista
            //logica para setear los datos de entrada
            decimal nMonDesemb = Convert.ToDecimal(SolicitudPlan.Rows[0]["nCapitalAprobado"]);
            decimal nTasa = Convert.ToDecimal(SolicitudPlan.Rows[0]["nTasaCompensatoria"]);//aqui sale 65 en entero
            nTasa = nTasa/100;                                                                               //
            DateTime dFecDesemb = Convert.ToDateTime(SolicitudPlan.Rows[0]["dFechaDesembolsoSugerido"]);
            int nNumCuoCta = Convert.ToInt32(SolicitudPlan.Rows[0]["nCuotas"]);
            int nDiaGraCta = Convert.ToInt32(SolicitudPlan.Rows[0]["ndiasgracia"]);
            short nTipPerPag;
            
            int nNumsolicitud = Convert.ToInt32(SolicitudPlan.Rows[0]["idSolicitud"]);
            DataTable dtConfigGasto;
            int nIdMoneda;
            DataTable dtCuotasDobles;
            DateTime dFecPrimeraCuota = Convert.ToDateTime(SolicitudPlan.Rows[0]["dFechaPrimeraCuota"]);
            int nDiaFecPag = dFecPrimeraCuota.Day;
            
            decimal nCuotaSugerida = 0;
            decimal nCapitalMaxCobSegDes = 0;
            int nNroCuotasGracia = 0;
            bool lConstante = true;
            decimal _nCuota = Convert.ToInt32(SolicitudPlan.Rows[0]["nCuotas"]);
            //clsSolicitudCreditoCargaSeguro objSolCargaSeguros = null;
            bool lAplicaNuevoMultirriesgo = true;
            bool lEsPrePago = false;
            int nCuotaMinimaCobroSeguro = 1;
            int nPlazoCuota = Convert.ToInt32(SolicitudPlan.Rows[0]["nPlazoCuota"]);
            ///////aqui estan los datos de la solicitud nada mas eso:
            ///-- en corebank la feh actal es 28/03/2025
            DateTime FechaCorebanck = new DateTime(2025, 3, 28); // Formato correcto: año, mes, día
            dFecDesemb = AjustarFechaDesembolso(dFecDesemb, FechaCorebanck, nDiaGraCta);
            ViewBag.dFecDesemb = FechaCorebanck; // Pasa a la vista
            
            ///setearemos las fechas de desembolso segun a l numero de dias y si hay dias de gracia tambien
            ///

            //dFecDesemb = CalculoFechaDesemsolso(dFecPrimeraCuota, nPlazoCuota, nDiaGraCta); esta solucion no gufnciona
            







            DataTable dtplanpago = new DataTable();
            //creamos el datable de plan pago

            #region Realizando el cálculo de la tasa efectiva diaria


            //Tasa de interes efectiva diaria TEA
            //decimal tasadiadria = 0.0013920104147622237m;

            decimal nTasEfeDia = (decimal)Math.Pow((double)(1 + nTasa), 1.0 / 360) - 1;

            //ViewBag.nTasEfeDia = nTasEfeDia; // Pasa el DataTable a la vista

            #endregion

            #region Realizando la definición de la tabla de plan de pagos
            int nDiaAcumul = 0;
            decimal nFacRecCap;
            decimal nFacAcumul = 0; 
            DateTime dFecNewCuo = dFecPrimeraCuota;
            //ViewBag.dFecPrimeraCuota = dFecPrimeraCuota; // Pasa el DataTable a la vista

            DataTable dtPlanPago = new DataTable("dtPlanPago");
            dtPlanPago.Columns.Add("cuota", typeof(int));
            dtPlanPago.Columns.Add("fecha", typeof(DateTime));
            dtPlanPago.Columns.Add("dias", typeof(int));
            dtPlanPago.Columns.Add("dias_acu", typeof(int));
            dtPlanPago.Columns.Add("frc", typeof(decimal));
            dtPlanPago.Columns.Add("sal_cap", typeof(decimal));
            dtPlanPago.Columns.Add("capital", typeof(decimal));
            dtPlanPago.Columns.Add("interes", typeof(decimal));
            dtPlanPago.Columns.Add("comisiones", typeof(decimal));
            dtPlanPago.Columns.Add("comisiones_sinSeg", typeof(decimal)); //comisiones sin seguro
            dtPlanPago.Columns.Add("itf", typeof(decimal));
            dtPlanPago.Columns.Add("nAtrasoCuota", typeof(int));
            dtPlanPago.Columns.Add("nInteresComp", typeof(decimal));
            dtPlanPago.Columns.Add("imp_cuota", typeof(decimal));
            dtPlanPago.Columns.Add("nIdSolicitud", typeof(int));

            #endregion

            #region Fecha Fija 
            // Para evitar que la primera cuota se genere el mismo día del desembolso (con 0 días)
            if (nDiaGraCta == 0)
            {
               dFecNewCuo = dFecNewCuo.AddDays(1);

            }
            
            //empezamos a generar la primeras filas para luyego poder acalulcar las demas
            for (int i = 1; i <= nNumCuoCta; i++) //Recorrer las cuotas para definir las fechas de pago
            {
                DataRow fila = dtPlanPago.NewRow();
                fila["cuota"] = i; //apertura la primera fila
                int nNumMesCuo; // 
                int nNumAñoCuo;
                if (i == 1) // Si primera cuota
                {
                    nNumMesCuo = dFecNewCuo.Month;
                    nNumAñoCuo = dFecNewCuo.Year;
                    if (nNumMesCuo > 12)
                    // Si el mes de la nueva cuota superó a diciembre se pone a enero y se suma un año
                    {
                        nNumMesCuo = 1;
                        nNumAñoCuo = nNumAñoCuo + 1;
                    }
                }
                else // A partir de la 2da cuota
                {
                    nNumMesCuo = dFecNewCuo.Month + 1;
                    nNumAñoCuo = dFecNewCuo.Year;
                    if (nNumMesCuo > 12)
                    // Si el mes de la nueva cuota superoo a diciembre se pone a enero y se suma un año
                    {
                        nNumMesCuo = 1;
                        nNumAñoCuo = nNumAñoCuo + 1;
                    }
                }
                var nDiaFecAux = nDiaFecPag; //el dia de la primera cuota
                //no es nesesario pero controlamos que la siguiente cuota que viene sea 100 valido y si no es lo restamos -1
                while (true) // La Fecha de la nueva cuota debe ser una fecha válida
                {
                    //si es la primera cuota
                    if (i == 1)
                    {
                        dFecNewCuo = dFecPrimeraCuota;
                        break;
                    }
                    DateTime dfecVeriFec;
                    //                                   25/03/2025      25             03         2025
                    if (DateTime.TryParse(string.Format("{0}/{1}/{2}", nDiaFecAux, nNumMesCuo, nNumAñoCuo),
                        out dfecVeriFec))
                    {
                        dFecNewCuo = DateTime.Parse(string.Format("{0}/{1}/{2}", nDiaFecAux, nNumMesCuo, nNumAñoCuo));
                        break;
                    }
                    nDiaFecAux = nDiaFecAux - 1;
                    
                    // si no es una fecha válida se retrocede hasta encontrar la primera fecha (ejem 31 de c/mes)
                }
                fila["fecha"] = dFecNewCuo;
                // calculando la cantidad de días para la cuota
                if (i == 1) // para la primera cuota
                {
                    fila["dias"] = (dFecNewCuo - dFecDesemb).Days;
                }
                else //Para las cuotas de la segunda en adelante
                {
                    fila["dias"] =
                        (Convert.ToDateTime(dFecNewCuo) - Convert.ToDateTime(dtPlanPago.Rows[i - 2]["fecha"])).Days;
                }

                nDiaAcumul = nDiaAcumul + Convert.ToInt32(fila["dias"]);
                fila["dias_acu"] = nDiaAcumul;

                //nFacRecCap = 1 / Convert.ToDecimal(Math.Pow((1 + nTasEfeDia), nDiaAcumul));
                //nFacRecCap  = 1 / (decimal)Convert.ToDecimal(Math.Pow((1 + nTasEfeDia), nDiaAcumul));
                //nFacRecCap = 1 / Convert.ToDecimal(Math.Pow((double)(1 + nTasEfeDia), nDiaAcumul));

                // Uso:
                //decimal factor = DecimalPow(1 + nTasEfeDia, nDiaAcumul);

                //calculando el FRC de la cuota
                // Cálculo mejorado
                //decimal tasadiadria = 0.0013920104147622237m;

                decimal factor = DecimalPow(1 + nTasEfeDia, nDiaAcumul);
                nFacRecCap = 1m / factor;
                

                //nFacRecCap = nFacRecCap * (lCuotaDoble ? 2 : 1);
                nFacAcumul = nFacAcumul + nFacRecCap; //Acumulando el FRC
                //nFacAcumulaux = nFacAcumulaux + nFacAcumulaux; //Acumulando el FRC

                decimal nFacAcumulmostrar = Math.Round(nFacRecCap, 2); ; //aqui registraos los dias de gracia

                //fila["frc"] = nFacRecCap;
                fila["frc"] = nFacAcumulmostrar;
                dtPlanPago.Rows.Add(fila);
            }
            

            #endregion

            #region Calculamos el algoritmo para Calcular plan paggos
            decimal nCuotaSugIni = CalcularCuotaSugerida(nMonDesemb, nFacAcumul, _nCuota);
            //aqui ya esta dstinto al corebanck
            //mi factor acum =9.237997948879822171347810446M
            //mi factor acum =9.237997948876128632564977521M

            decimal nMonCuoAux = nCuotaSugIni;
            int nNumIterac = 0;
            decimal nMonSalCap = decimal.Round(nMonDesemb, 2);

            const int nMaxIterac = 20;
            decimal nValErrMax = 0.09m * nNumCuoCta;

            bool lFlgFactor = false;
            int nIteraTrue = 0;

            bool lIndSalIte = false;
            int lNumSalIte = 0;
            decimal nPotencDos = 0;
            decimal nRazBusCuo = 0;
            decimal nMontoCuota;
            decimal nOtrosCuota;
            int nDecRedonCalcPpg =1 ;
            decimal nValorSegurosOptativos = 0;
            /*variable de apoyo*/
            decimal nMenorDiferencia = 99999999;

            /*dt temporal para seleccionar plan de pagos*/
            DataTable dtPlanPagosSeleccionado = new DataTable();

            //ViewBag.Datoaux = nValErrMax; // Pasa el DataTable a la vista
            if (lConstante)
            {
                nMontoCuota = nMonCuoAux;
                //va dar vueota 20 veces
                while (Math.Abs(nMonSalCap) > nValErrMax && nNumIterac < nMaxIterac)
                {
                    
                    nMonSalCap = decimal.Round(nMonDesemb, 2);
                    //traemos el deguro desgravamen-- para montos menoes de 10k el prestamo el segurodesgravamen es
                    decimal segurDesgrav = 0.1650m;

                    //iniciarDTGastos();  debemos poder calcular los demas gastos aqui, seguros y mas tiposq eu tenga en la solicicutd
                    
                    foreach (DataRow row in dtPlanPago.Rows)
                    {
                        int idCuota = Convert.ToInt32(row["cuota"]);
                        DataRow drCuotaAnt = null;
                        // entra desspues de la primera fila

                        if (idCuota > 1)
                        {
                            drCuotaAnt = dtPlanPago.Rows[dtPlanPago.Rows.IndexOf(row) - 1];
                        }
                        int nDiaCuoCta = Convert.ToInt16(row["dias"]);//28
                         //interes de los 28 dias para la primera cuota Interes=SaldoCapital×((1+TasaDiaria)Dıas −1)
                         //en la formula general lo hace a la tasa mendual pero en corebanck lo  hace a la tasa diarai
                        decimal nMonIntCuo =
                            //decimal.Round((nMonSalCap * (decimal)(Math.Pow(1 + nTasEfeDia, nDiaCuoCta) - 1)), 2);
                           decimal.Round(nMonSalCap * (DecimalPow(1m + nTasEfeDia, nDiaCuoCta) - 1m),2,MidpointRounding.AwayFromZero); //dese ser 0
                        //dia de la primera cuota
                        DateTime dFecha = row.Field<DateTime>("fecha");
                        //Redondeamos a 1 decimal
                        decimal nMontCuoRedond = Math.Round(nMonCuoAux, nDecRedonCalcPpg);
                        
                        row["sal_cap"] = nMonSalCap;
                        row["interes"] = nMonIntCuo;
                        row["comisiones_sinSeg"] = 0;
                        row["itf"] = 0;
                        row["imp_cuota"] = nMontCuoRedond;
                        row["nIdSolicitud"] = nNumsolicitud;

                        //calculamos el seguro desgravamen y lo convertimos a decimales
                        nOtrosCuota =  Math.Round(segurDesgrav*nMonSalCap/100, 3);
                        //-----------------------------------------------------si hay seguros podemos cargarlos aqui
                        //si el seguro optativo es educativo
                        decimal segurooptativo = 2.5m;
                        nValorSegurosOptativos =segurooptativo;

                        row["comisiones"] = nOtrosCuota + nValorSegurosOptativos; // en corebank trabaja con 4.950.
                        //row["comisiones"] = nValorSegurosOptativos;

                        decimal nOtros = Convert.ToDecimal(row["comisiones"]);
                        decimal nMonCapCuo = (nNroCuotasGracia >= idCuota) ? 0.0m : nMontCuoRedond - nMonIntCuo - nOtros; //trabaja 200.60 pero en corebank con 4 ceros0´mas
                        //lo convierto a 2 decimales
                        nMonCapCuo = Math.Round(nMonCapCuo,2);
                        row["capital"] = nMonCapCuo;

                        //objCargaGastosCred.CargarGastoCuotaNoConstantes(row, drCuotaAnt);
                        nOtrosCuota = Math.Round(segurDesgrav*nMonSalCap/100, 2);

                        row["comisiones"] = nValorSegurosOptativos+nOtrosCuota;
                        //row["comisiones"] = nOtrosCuota + nValorSegurosOptativos;
                        //esto lohace porgusto por que arria ya tengo todo planeado.   --- aqui imprime mal... ineesssesario -..........................
                        row["imp_cuota"] = nMonCapCuo + nMonIntCuo +nOtrosCuota+ nValorSegurosOptativos;

                        nMonSalCap = nMonSalCap - nMonCapCuo;
                    }

                    nMontoCuota = Math.Round(nMonCuoAux, nDecRedonCalcPpg);
                    nNumIterac = nNumIterac + 1;

                    ////si esta seteado la cuota <> 0 entonces no se itera
                    //if (_nCuota > 0)
                    //{
                    //    break;
                    //}
                    //// aqui empezamos a calcular los siguientes valores de importe cuota
                    //si esta seteado la cuota <> 0 entonces no se itera pero si itera
                    if (nIteraTrue > 0)
                    {
                        if (nMonSalCap < 0)
                        {
                            nPotencDos = nPotencDos / 2;
                            nMonCuoAux = nCuotaSugIni - nRazBusCuo;
                            lFlgFactor = true;
                        }
                        else
                        {
                            if (lFlgFactor == false)
                            {
                                nPotencDos = nPotencDos * 2;
                            }
                        }
                    }
                    else
                    {
                        nPotencDos = 2;
                    }

                    nIteraTrue = nIteraTrue + 1;
                    //empiezza a bucar el R en aproximaciones
                    nRazBusCuo = decimal.Round(nMonSalCap * nPotencDos / nDiaAcumul, 10);
                    nMonCuoAux = nMonCuoAux + nRazBusCuo;
                    //si R es = o entonces entra
                    if (nRazBusCuo == 0)
                    {
                        if (lIndSalIte == false)
                        {
                            lIndSalIte = true;
                            lNumSalIte = nNumIterac;
                        }

                        nMonCuoAux = nMonCuoAux + 0.01m;
                    }
                    //
                    if ((nNumIterac == lNumSalIte + 1) && lNumSalIte > 0)
                    {
                        break;
                    }

                    /*Seleccion de Plan de Pagos con menor diferencia entre Saldo Capital y Capital*/
                    decimal nMenorDiferencia_ =
                        Math.Abs(Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["sal_cap"])
                        - Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["capital"]));
                    if (nMenorDiferencia >= nMenorDiferencia_)
                    {
                        nMenorDiferencia = nMenorDiferencia_;
                        //copiamos el datatable que contruimos 
                        dtPlanPagosSeleccionado = dtPlanPago.Copy();
                    }
                }
                /*Seteado de Pan de Pagos seleccionado*/
                dtPlanPago = dtPlanPagosSeleccionado.Copy();
            }
            ////Ajuste de la última cuota
            decimal nSumCapital = 0;

            int nNumDiaUltCuo = Convert.ToInt16(dtPlanPago.Rows[nNumCuoCta - 1]["dias"]);//28
            decimal nSaldoCapUltcCuo = Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["sal_cap"]); //saldo capital 315.30
            decimal nCapUltCuo = nSaldoCapUltcCuo; //
            decimal Capital = Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["capital"]);
            decimal utilmaCuota = Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["imp_cuota"]);
            //decimal imporcuota = Convert.ToDecimal(dtPlanPago.Rows[nNumCuoCta - 1]["capital"]);


            dtPlanPago.Rows[nNumCuoCta - 1]["capital"] = Math.Round(nCapUltCuo, 2);
            //dtPlanPago.Rows[nNumCuoCta - 1]["interes"] = Math.Round(nCapUltCuo, 2);
            //dtPlanPago.Rows[nNumCuoCta - 1]["imp_cuota"] = Math.Round(nCapUltCuo + nCapUltCuo + nMonOtrosUltCuo, 2);
            dtPlanPago.Rows[nNumCuoCta - 1]["imp_cuota"] = Math.Round(utilmaCuota+nCapUltCuo-Capital, 2);

            //tGastos = objCargaGastosCred.dtGastos.Copy();



            dtPlanPago.AcceptChanges();

            //turn dtPlanPago;
            ////////quitar filas que no intervienen en uan cuota normal
            dtPlanPago.Columns.Remove("nAtrasoCuota");
            dtPlanPago.Columns.Remove("nInteresComp");
            dtPlanPago.Columns.Remove("nIdSolicitud");
            dtPlanPago.Columns.Remove("comisiones_sinSeg");
            ViewBag.dtPlanPago = dtPlanPago; // Pasa el DataTable a la vista
            #endregion

            return PartialView("PlanPagosPartial");
        }
        // Método para calcular potencias con decimales
        static decimal DecimalPow(decimal baseValue, int exponent)
        {
            decimal result = 1m;
            for (int i = 0; i < exponent; i++)
                result *= baseValue;
            return result;
        }
        private decimal CalcularCuotaSugerida(decimal nMonDesemb, decimal nFacAcumul, decimal _nCuota)
        {
            return nMonDesemb / nFacAcumul;
            //return decimal.Round(nMonDesemb / nFacAcumul);
        }

        //private DateTime AjustarFechaDesembolso(DateTime fechaDesembolso, DateTime fechaCorebanck)
        //{
        //    // Si la fecha de desembolso es anterior a Corebanck, se usa Corebanck
        //    return (fechaDesembolso < fechaCorebanck) ? fechaCorebanck : fechaDesembolso;
        //}
        private DateTime AjustarFechaDesembolso(DateTime fechaDesembolso, DateTime fechaCorebanck, int nDiaGraCta)
        {
            DateTime fechaLimite = fechaCorebanck.AddDays(-nDiaGraCta); // Resta días de gracia
            return (fechaDesembolso < fechaLimite) ? fechaCorebanck : fechaDesembolso;
        }
        //*********************aq******



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }


    }
}
