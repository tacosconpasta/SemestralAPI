using Npgsql;
using SemestralAPI.Models;
using System.Data;
using System.Numerics;

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

    //Retorna un objeto (Cliente o Usuario[Admin])
    public object IniciarSesion(string usuario, string contrasena) {
      if (string.IsNullOrEmpty(usuario) | string.IsNullOrEmpty(contrasena)) {
        return null;
      }

      try {
        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Construir sentencia
        _cmd.CommandType = System.Data.CommandType.Text;
        _cmd.CommandText = "SELECT id, usuario, contrasena, rol, cliente_id FROM usuario WHERE usuario = @usuario AND contrasena = @contrasena";
        _cmd.Parameters.AddWithValue("@usuario", usuario);
        _cmd.Parameters.AddWithValue("@contrasena", contrasena);

        //Abrir conexión
        _cmd.Connection.Open();

        //Dataset y adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar query
        adapter.SelectCommand = _cmd;

        //Rellenar ds con datos
        adapter.Fill(ds);

        //Cerrar Conexión
        _cmd.Connection.Close();

        //Si ningún registro coincidió, retornar null
        if (ds.Tables[0].Rows.Count <= 0) {
          return null;
        }

        //Identificar si el usuario es un cliente o un administrado
        string rol = ds.Tables[0].Rows[0]["rol"].ToString()!;

        //Si el usuario es un cliente
        if (ds.Tables[0].Rows[0]["cliente_id"].GetType() != typeof(DBNull) && rol == "cliente") {
          int cliente_id = Convert.ToInt32(ds.Tables[0].Rows[0]["cliente_id"]);
          Cliente clienteARetornar = BuscarCliente(cliente_id);

          //Si no se encontró un cliente, retornar null
          if (clienteARetornar == null) {
            return null;
          }
          return clienteARetornar;

        }

        //Si el usuario no es un cliente, es un administrador
        Usuario usuarioAdministrador = new Usuario();

        //Construir usuario
        usuarioAdministrador.User = ds.Tables[0].Rows[0]["usuario"].ToString()!;
        usuarioAdministrador.Rol = rol;
        usuarioAdministrador.Id = Convert.ToInt32(ds.Tables[0].Rows[0]["id"]);

        return usuarioAdministrador;
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        return null;
      } finally {
        _cmd.Connection!.Close();
      }
    }

    public Cliente BuscarCliente(int cliente_id) {
      //Limpiar parametros de anteriores querys
      _cmd.Parameters.Clear();

      //Construir sentencia
      _cmd.CommandType = System.Data.CommandType.Text;
      _cmd.CommandText = "SELECT nombre, apellido, telefono, correo, direccion FROM cliente WHERE id = @cliente_id";
      _cmd.Parameters.AddWithValue("@cliente_id", cliente_id);

      //Abrir conexión
      _cmd.Connection!.Open();

      //Dataset y adapter
      DataSet ds = new DataSet();
      NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

      //Ejecutar query
      adapter.SelectCommand = _cmd;

      //Rellenar ds con datos
      adapter.Fill(ds);

      //Cerrar Conexión
      _cmd.Connection.Close();

      //Crear cliente a devolver
      Cliente cliente = new Cliente();

      //No se encontró cliente
      if (ds.Tables[0].Rows.Count <= 0) {
        return null;
      }

      //Asignar datos a cliente
      cliente.Nombre = ds.Tables[0].Rows[0]["nombre"].ToString();
      cliente.Apellido = ds.Tables[0].Rows[0]["apellido"].ToString();
      cliente.Telefono = ds.Tables[0].Rows[0]["telefono"].ToString();
      cliente.Correo = ds.Tables[0].Rows[0]["correo"].ToString();
      cliente.Direccion = ds.Tables[0].Rows[0]["direccion"].ToString();

      return cliente;
    }
  }
}
