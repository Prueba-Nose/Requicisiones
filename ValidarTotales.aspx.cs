using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.ConstrainedExecution;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Ajax_ValidarTotal : System.Web.UI.Page
{

    protected void Page_Load(object sender, EventArgs e)
    {
        if (Request.HttpMethod == "POST")
        {
            string enviarEmail = "0";
            int idreq, noAprobacion;
            string area, ip, obs, correo;
            // Recorre todas las claves del formulario
            foreach (string key in Request.Form.AllKeys)
            {
                // Verifica si la clave comienza con "ruta" (o cualquier prefijo que estés usando)
                if (key.StartsWith("aprobar"))
                {
                    // Obtiene el valor asociado a la clave
                    string valor = Request.Form[key];
                    string[] aprobados = valor.Split(',');

                    for (int i = 0; i < aprobados.Length; i++)
                    {
                        idreq = int.Parse(aprobados[i]);
                        noAprobacion = int.Parse(Request.Form["ruta" + idreq]);
                        area = Request.Form["area" + idreq];
                        ip = Request.Form["ip" + idreq];

                        if (ValidarTotalRequi(idreq, noAprobacion, area, ip) == "1")
                        {
                            enviarEmail = "1";
                        }
                    }
                }
                else if (key.StartsWith("rechazar"))
                {
                    // Obtiene el valor asociado a la clave
                    string valor = Request.Form[key];
                    string[] rechazados = valor.Split(',');
                    foreach (string id in rechazados)
                    {
                        idreq = int.Parse(id);
                        noAprobacion = int.Parse(Request.Form["ruta" + idreq]);
                        area = Request.Form["area" + idreq];
                        ip = Request.Form["ip" + idreq];
                        obs = Request.Form["observacion" + idreq];
                        Usuarios userR = new Usuarios()
                        {
                            id = int.Parse(Request.Form["nom" + idreq])
                        };
                        userR.ObtenerUsuario();
                        correo = userR.Email;
                        //Response.Write(idreq + " " + noAprobacion + " " + obs + " " + correo + " " + ip + " " + area);
                        RechazarReq(idreq, noAprobacion, obs, correo, ip, area);
                        enviarEmail = "1";
                    }
                }
            }
            if (enviarEmail == "1")
            {
                Response.Redirect("enviar.asp?asunto=Oca&pagina=ApOca&rech=rechazada&usuario=" + Request.Cookies["adm"]["idUsuario"]);
            }
            else
            {
                Response.Redirect("requi.asp");
            }
        }
    }

    public string ValidarTotalRequi(int idreq, int noAprobacion, string area, string ip)
    {
        //Establece un valor predefinido de 0 para el correo
        string enviarEmail = "0";
        //Consulta la requisicion
        Requisicion req = new Requisicion()
        {
            id = idreq
        };
        req.obtenerRequi();
        double totalReq = req.ObtenerTotal();

        Logger.Log(String.Format("Total Requi inicial: {0}", totalReq));

        //Crea la instancia para los log de aprobacion
        LogAprobacion logRequi = new LogAprobacion()
        {
            idReq = idreq,
            noAprobacion = noAprobacion,
            tipo = "Requisicion"
        };
        logRequi.ObtenerRutaAprobacion();

        // Crea la instancia para consultar los usuarios en la ruta de aprobacion 
        UsuariosRuta user = new UsuariosRuta()
        {
            idRuta = logRequi.idRuta,
            noAprobacion = noAprobacion,
            tipo = "Requisicion"
        };
        int max = user.ObtenerMaximoRuta();
        user.ObtenerUsuario();

        Logger.Log(String.Format("Usuario actual ID: {0}, Total usuario: {1}", user.idUsuario, user.total));

        //Consultar siguiente usuario
        int noAprobacionNext = noAprobacion + 1;
        logRequi.noAprobacion = noAprobacionNext;
        logRequi.tipo = "Requisicion";
        logRequi.ObtenerRutaAprobacion();
        int idRuta = logRequi.idRuta;

        //Crea la instancia de los siguientes usuarios 
        UsuariosRuta userNext = new UsuariosRuta()
        {
            idRuta = idRuta,
            noAprobacion = noAprobacionNext,
            tipo = "Requisicion"
        };

        userNext.ObtenerUsuario(); // Aseguramos que esta linea del codigo se ejecute correctamente

        //Crea la instancia para buscar los datos del siguiente usuario en la ruta de aprobacion
        Usuarios usuario = new Usuarios()
        {
            id = userNext.idUsuario
        };
        usuario.ObtenerUsuario();

        //Crea la instancia para preparar el log de aprobacion 
        LogAprobacion logAp = new LogAprobacion()
        {
            idReq = idreq,
            tipo = "Requisicion"
        };

        Logger.Log(String.Format("Siguiente usuario ID: {0}, Total usuario siguiente: {1}", userNext.idUsuario, userNext.total));

        //Valida si la autorizacion actual es la maxima de la ruta 
        if (max == noAprobacion)
        {
            //Aprueba y acepta la Oc
            logAp.ObtenerUltimoAprobacion();
            logAp.AprobarRequisicion();
            req.AceptarRequisicion();
        }
        else
        {
            //Aprueba usuario
            logRequi.AprobarRequisicion();

            //Crea la instancia para las siguientes aprobaciones
            LogAprobacion log = new LogAprobacion
            {
                estatus = "Aprobacion" + noAprobacionNext,
                idReq = idreq,
                tipo = "Requisicion",
                idRuta = idRuta,
                noAprobacion = noAprobacionNext,
                totalRA = user.total
            };
            log.InsertarLog();
            log.ObtenerUltimoAprobacion();

            //verifica si la siguiente aprobacion es menor que la maxima, si lo es, envia correo
            if (noAprobacionNext <= max)
            {
                string correo = "";
                string mensaje = "Arcanet-Compras/traduce1.asp?cv=Aprobacion" + userNext.noAprobacion + area;
                correo = usuario.Email;
                //Enviar correo de autorizacion al usuario 
                req.InsertarLogCorreoTotales(user.idUsuario, correo, mensaje, ip, "Aprobacion" + user.noAprobacion.ToString());
                string mensajeCorreo = @"<br /><a href=http://" + ip + "/" + mensaje + ">Requisition for Authorization link</a><br /><br />" +
                    "Fecha y hora : " + DateTime.Now.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss");
                //SendEmail(correo, "Sistema de Ordenes de Compra (Autorización de Requisición)", mensajeCorreo);
                enviarEmail = "1";
            }
            req.noApr = noAprobacionNext;
            req.AprobarRequisicion();
        }

        //Crea la instancia para las monedas 
        Moneda moneda = new Moneda()
        {
            id = req.idMoneda
        };
        moneda.ObtenerMoneda();

        Moneda monedareq = new Moneda()
        {
            id = idreq
        };
        monedareq.ObtenerMonedaReq();
        monedareq.ObtenerTipoCambiodls();


        Moneda monedaUsuario = new Moneda()
        {
            id = userNext.moneda
        };
        monedaUsuario.ObtenerMoneda();

        //Establece el total de la orden en una variable 
        double totalOrden = totalReq;

        //Validar monedas
        //Moneda de la requi no es MXP o MXN
        if (monedareq.tipomoneda == "USD")
        {
            double tipoCambio = monedareq.ObtenerTipoCambiodls();
            totalOrden *= tipoCambio;
        }
        else if (monedareq.tipomd == "MXP")
        {
            totalOrden *= 1;
        }
        if (!(monedaUsuario.tipomd == "MXP" || monedaUsuario.tipomd == "MXN"))
        {
            double tipoCambioUsuario = monedaUsuario.ObtenerTipoCambio();
            userNext.total *= tipoCambioUsuario;
        }

        Logger.Log(String.Format("Total Requi convertido: {0}, Total usuario siguiente convertido: {1}", totalOrden, userNext.total));

        //Validar que el total de la requi con el total del usuario siguiente
        if (totalOrden < userNext.total)
        {

            Logger.Log(String.Format("Total REQUI {0} es menor que Total Usuario Siguiente {1}, se procederá a validar la compra.", totalOrden, userNext.total));

            LogAprobacion log = new LogAprobacion();

            if (max == noAprobacionNext)
            {
                logRequi.AprobarRequisicion();
                req.AceptarRequisicion();
                req.AprobarRequisicion();
            }

            int noAprobacionNext2 = noAprobacion + 2;
            logRequi.noAprobacion = noAprobacionNext2;
            logRequi.tipo = "Requisicion";
            logRequi.ObtenerRutaAprobacion();
            int idRuta2 = logRequi.idRuta;

            LogAprobacion log2 = new LogAprobacion
            {
                estatus = "Aprobacion" + noAprobacionNext2,
                idReq = idreq,
                tipo = "Requisicion",
                idRuta = idRuta2,
                noAprobacion = noAprobacionNext2,
                observaciones = String.Format("El total del usuario es {0}", userNext.total)
            };

            if (noAprobacionNext2 < max)
            {
                UsuariosRuta userNext2 = new UsuariosRuta()
                {
                    idRuta = idRuta,
                    noAprobacion = noAprobacionNext2,
                    tipo = "Requisicion",
                };

                userNext.ObtenerUsuario();

                Usuarios usuario3 = new Usuarios()
                {
                    id = userNext2.idUsuario
                };

                string correo = "";
                string mensaje = "Arcanet-Compras/traduce1.asp?cv=Aprobacion" + userNext2.noAprobacion + area;
                correo = usuario3.Email;
                //Enviar correo de autorizacion al usuario 
                req.InsertarLogCorreoTotales(user.idUsuario, correo, mensaje, ip, "Aprobacion" + userNext2.noAprobacion.ToString());
                string mensajeCorreo = String.Format(@"<br /><a href=http://{0}/{1}>Purchase Order for Authorization link</a><br /><br />Fecha y hora : {2}", ip, mensaje, DateTime.Now.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                //SendEmail(correo, "Sistema de Ordenes de Compra (Autorización de Orden de Compra)", mensajeCorreo);
                enviarEmail = "1";

                log2.InsertarLog();
            }
        }
        else
        {
            if (noAprobacionNext == max)
            {
                string correo = "";
                string mensaje = "Arcanet-Compras/traduce1.asp?cv=Aprobacion" + userNext.noAprobacion + area;
                correo = usuario.Email;
                //Enviar correo de autorizacion al usuario 
                req.InsertarLogCorreoTotales(user.idUsuario, correo, mensaje, ip, "Aprobacion" + userNext.noAprobacion.ToString());
                string mensajeCorreo = String.Format(@"<br /><a href=http://{0}/{1}>Purchase Order for Authorization link</a><br /><br />Fecha y hora : {2}", ip, mensaje, DateTime.Now.ToUniversalTime().ToString("dd/MM/yyyy HH:mm:ss"));
                //SendEmail(correo, "Sistema de Ordenes de Compra (Autorización de Orden de Compra)", mensajeCorreo);
                enviarEmail = "1";
            }
        }

        return enviarEmail;
    }

    public void SendEmail(string destinatario, string asunto, string mensaje)
    {
        try
        {
            Config conf = new Config()
            {
                id = 1
            };
            conf.ObtenerConfig();
            // Configuración del cliente SMTP
            SmtpClient smtpClient = new SmtpClient(conf.smtpenv);
            smtpClient.Port = conf.puerto; // Puerto del servidor SMTP (puedes cambiarlo según el proveedor)
            smtpClient.Credentials = new NetworkCredential(conf.correoenv, conf.passwordenv);
            // Crear el mensaje
            mensaje += conf.mensajedos;
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(conf.correoVista); // Tu dirección de correo electrónico
            mailMessage.To.Add(destinatario);
            mailMessage.Subject = asunto;
            mailMessage.Body = mensaje;
            mailMessage.IsBodyHtml = true; // Si el cuerpo es HTML, establecer en true

            // Enviar el correo
            smtpClient.Send(mailMessage);

        }
        catch (Exception ex)
        {
        }
    }

    public void RechazarReq(int idreq, int noAprobacion, string obs, string correo, string ip, string area)
    {
        LogAprobacion log = new LogAprobacion()
        {
            idReq = idreq,
            tipo = "Requisicion",
            noAprobacion = noAprobacion,
            observaciones = obs
        };
        log.ObtenerRutaAprobacion(); //Obtener id del log

        //Rechazar en logAprobacion
        log.Rechazar();

        //Rechazar en Ocompra
        Requisicion req = new Requisicion()
        {
            id = idreq
        };
        req.RechazarRequisicion();

        //Insertar en email
        UsuariosRuta userLog = new UsuariosRuta()
        {
            idRuta = log.idRuta,
            noAprobacion = noAprobacion,
            tipo = "Requisicion"
        };
        userLog.ObtenerUsuario();
        string mensaje = "Arcanet-Compras/traduce2.asp?cv=Aprobacion" + userLog.noAprobacion + area;
        req.InsertarLogCorreoTotales(userLog.idUsuario, correo, mensaje, ip, "Rechazado");
    }
}