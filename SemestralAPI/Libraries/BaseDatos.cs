using Npgsql;

namespace SemestralAPI.Libraries {
  public class BaseDatos {
    private readonly string _connectionString;
    private NpgsqlCommand _cmd = new NpgsqlCommand();

    //Constructor 1: Parametros Individuales
    public BaseDatos(string host, string database, string username, string password, int port = 5432) {
      //Safe Password (Forzar atributo cadena aunque la contraseña tenga caracteres especiales: ", /, _, %
      string safePassword = $"\"{password}\"";


      _connectionString =
          $"Host={host};Port={port};Database={database};Username={username};Password={safePassword};";

      _cmd.Connection = new NpgsqlConnection(_connectionString);
    }

    //Constructor 2: Cadena de Conexión
    public BaseDatos(string connectionString) {
      _connectionString = connectionString;
    }

    //Prueba Conexión
    public bool ProbarConexion() {
      try {
        //Limpiar parámetros de ejecuciones anteriores
        _cmd.Parameters.Clear();

        //Abrir conexión de prueba
        _cmd.Connection?.Open();

        //Retornar cierto, si la conexión fue exitosa
        return true;

        //Loggear Excepción, retornar falso
      } catch (Exception ex) {
        return false;

        //Cerrar conexión
      } finally {
        _cmd.Connection?.Close();
      }
    }


  }
}
