using System;
using System.Collections.Generic;
using System.Web;
using System.Configuration;
using System.Linq;
using System.Data;
using System.Data.SqlClient;

/// <summary>
/// Descripción breve de UsuariosRuta
/// </summary>
public class UsuariosRuta
{
    //variables
    private string cadena;

    //propiedades
    public int id { get; set; }
    public int idRuta { get; set; }
    public int noAprobacion { get; set; }
    public int idUsuario { get; set; }
    public string tipo { get; set; }
    public string puesto { get; set; }
    public double forecast { get; set; }
    public double total { get; set; }
    public double total2 { get; set; }
    public int moneda { get; set; }
    public DateTime fechaAlta { get; set; }
    public UsuariosRuta()
    {
        cadena = ConfigurationManager.ConnectionStrings["conexion"].ConnectionString;
    }

    public bool ObtenerUsuario()
    {
        try
        {
            string comando = "SELECT * FROM dbo.ruta2Detalles WHERE idRuta = @idRuta AND noAprobacion = @noAprobacion AND tipo = @tipo ORDER BY noAprobacion;";
            using (SqlConnection conn = new SqlConnection(cadena))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(comando, conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@idRuta", idRuta));
                    cmd.Parameters.Add(new SqlParameter("@noAprobacion", noAprobacion));
                    cmd.Parameters.Add(new SqlParameter("@tipo", tipo));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {

                                id = int.Parse(reader["idRutaDetalle"].ToString());
                                idUsuario = int.Parse(reader["idUsuario"].ToString());
                                double totalParse;
                                if (double.TryParse(reader["total"].ToString(), out totalParse))
                                {
                                    total = totalParse;
                                }
                                else
                                {
                                    total = 0;
                                }
                                double forecastParse;
                                if (double.TryParse(reader["forecast"].ToString(), out forecastParse))
                                {
                                    forecast = forecastParse;
                                }
                                else
                                {
                                    forecast = 0;
                                }
                                moneda = int.Parse(reader["moneda"].ToString());
                                puesto = reader["puesto"].ToString();
                                fechaAlta = DateTime.Parse(reader["fechaAlta"].ToString());
                            }
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {

            throw (ex);
        }
    }

    public int ObtenerMaximoRuta()
    {
        try
        {
            int maximo = 0;
            string comando = "SELECT MAX(noAprobacion) as maximo FROM dbo.ruta2Detalles WHERE idRuta = @idRuta AND tipo = @tipo";
            using (SqlConnection conn = new SqlConnection(cadena))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(comando, conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@idRuta", idRuta));
                    cmd.Parameters.Add(new SqlParameter("@tipo", tipo));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                maximo = int.Parse(reader["maximo"].ToString());
                            }
                            return maximo;
                        }
                        else
                        {
                            return maximo;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {

            return 0;
        }
    }

    public void GetStatusAprobacion()
    {
        try
        {
            string comando = "SELECT puesto FROM ruta2Detalles WHERE idRuta = @idRuta AND noAprobacion = @noAprobacion AND tipo = @tipo";
            using (SqlConnection conn = new SqlConnection(cadena))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(comando, conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@idRuta", idRuta));
                    cmd.Parameters.Add(new SqlParameter("@noAprobacion", noAprobacion));
                    cmd.Parameters.Add(new SqlParameter("@tipo", tipo));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                puesto = reader["puesto"].ToString();
                            }

                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {

            throw (ex);
        }
    }

    //Reportes
    public int[][] GetRutaAndAprobacionUser()
    {
        try
        {
            List<int[]> lista = new List<int[]>();
            string comando = "SELECT ruta2Detalles.idRuta AS ruta, noAprobacion FROM ruta2Detalles WHERE idUsuario = @idusuario AND tipo = 'Ocompra' ";
            using (SqlConnection conn = new SqlConnection(cadena))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(comando, conn))
                {
                    cmd.Parameters.Add(new SqlParameter("@idusuario", idUsuario));
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                int ruta = reader.GetInt32(0); // Obtener la ruta
                                int noAprobacion = reader.GetInt32(1); // Obtener el noAprobacion
                                lista.Add(new int[] { ruta, noAprobacion }); // Agregar a la lista
                            }
                            return lista.ToArray();
                        }
                        else
                        {
                            return null;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {

            return null;
        }
    }

    public bool ObtenerSiguienteUsuario()
    {
        try
        {
            noAprobacion++; // Incrementa el número de aprobación para obtener el siguiente usuario
            return ObtenerUsuario();
        }
        catch (Exception ex)
        {
            throw (ex);
        }
    }
}
