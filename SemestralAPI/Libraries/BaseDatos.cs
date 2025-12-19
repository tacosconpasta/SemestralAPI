using Npgsql;
using SemestralAPI.Models;
using System.Data;

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
    public BaseDatos(string connectionString) {
      _connectionString = connectionString;
      _cmd.Connection = new NpgsqlConnection(_connectionString);
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

    //Inserta un usuario, funciona para clientes y administradores
    public bool RegistrarUsuario(Usuario usuarioRegistrar) {
      try {
        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Abrir conexión
        _cmd.Connection.Open();

        //Si el usuario es un admin
        if (usuarioRegistrar.Rol == "admin") {
          //Constuir INSERT para admin
          _cmd.CommandText = @"INSERT INTO usuario (usuario, contrasena, rol, cliente_id) VALUES (@usuario, @contrasena, 'admin', NULL);";
          _cmd.Parameters.AddWithValue("@usuario", usuarioRegistrar.User);
          _cmd.Parameters.AddWithValue("@contrasena", usuarioRegistrar.Contrasena);
          _cmd.Parameters.AddWithValue("@rol", usuarioRegistrar.Rol);

          //Si el usuario a registrar es un cliente
        } else if (usuarioRegistrar.Rol == "cliente") {
          //Si no tiene cliente_id
          if (usuarioRegistrar.ClienteId == null)
            throw new Exception("Un usuario cliente debe tener cliente_id.");

          //Construir Query para Cliente
          _cmd.CommandText = @"INSERT INTO usuario (usuario, contrasena, rol, cliente_id) VALUES (@usuario, @contrasena, @rol, @cliente_id);";
          _cmd.Parameters.AddWithValue("@usuario", usuarioRegistrar.User);
          _cmd.Parameters.AddWithValue("@contrasena", usuarioRegistrar.Contrasena);
          _cmd.Parameters.AddWithValue("@rol", usuarioRegistrar.Rol);
          _cmd.Parameters.AddWithValue("@cliente_id", usuarioRegistrar.ClienteId);
        } else {
          throw new Exception("El usuario no posee un rol conocido");
        }

        //Ejecutar
        int result = _cmd.ExecuteNonQuery();

        //Si fue inválido
        if (result == 0)
          return false;

        return true;
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        return false;
      } finally {
        _cmd.Connection.Close();
      }
    }

    //Registrar un usuario cliente
    public bool RegistrarCliente(Usuario informacionUsuario, Cliente informacionCliente) {
      try {
        //Verificar si existe usuario
        if (ExisteUsuario(informacionUsuario.User) || ExisteCliente(informacionCliente))
          return false;

        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Abrir conexión
        _cmd.Connection.Open();

        //Construir sentencia (Insertar Cliente)
        _cmd.CommandType = System.Data.CommandType.Text;
        _cmd.CommandText = "INSERT INTO cliente(nombre, apellido, direccion, telefono, correo) VALUES (@nombre, @apellido, @direccion, @telefono, @correo) RETURNING id";
        _cmd.Parameters.AddWithValue("@nombre", informacionCliente.Nombre);
        _cmd.Parameters.AddWithValue("@apellido", informacionCliente.Apellido);
        _cmd.Parameters.AddWithValue("@direccion", informacionCliente.Direccion);
        _cmd.Parameters.AddWithValue("@telefono", informacionCliente.Telefono);
        _cmd.Parameters.AddWithValue("@correo", informacionCliente.Correo);

        //Ejecutar operación y obtener cliente_id ("ExecuteSalar" devuelve id de fila insertada|updateada)
        var scalarResult = _cmd.ExecuteScalar();
        int clienteId = Convert.ToInt32(scalarResult);

        //Asignar cliente_id a usuario
        informacionUsuario.ClienteId = clienteId;

        //Cerrar conexión para siguiente inserción (Usuario)
        _cmd.Connection.Close();

        //Limpiar parámetros para siguiente inserción (Usuario)
        _cmd.Parameters.Clear();

        //Registrar usuario
        bool resultRegistroUsuario = RegistrarUsuario(informacionUsuario);

        //Si el resultado fue 0, retornar falso, la operación no fue exitosa
        if (!resultRegistroUsuario) {
          return false;
        }

        return true;
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.ToString());
        return false;
      } finally {
        _cmd.Connection!.Close();
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

    //Verifica si el usuario existe,
    //retorna "true;" si existe.
    public bool ExisteUsuario(string usuario) {
      try {
        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Construir sentencia
        _cmd.CommandType = System.Data.CommandType.Text;
        _cmd.CommandText = "SELECT id, usuario, contrasena, rol, cliente_id FROM usuario WHERE usuario = @usuario;";
        _cmd.Parameters.AddWithValue("@usuario", usuario);

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

        //Si ningún registro coincidió, retornar falso
        if (ds.Tables[0].Rows.Count <= 0) {
          return false;
        }

        //Si existe registro, retornar cierto
        return true;
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        return false;
      } finally {
        _cmd.Connection.Close();
      }
    }

    //Verifica si el cliente existe
    public bool ExisteCliente(Cliente cliente) {
      try {
        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Verificar si existe por correo
        _cmd.CommandType = System.Data.CommandType.Text;
        _cmd.CommandText = "SELECT correo FROM cliente WHERE correo = @correo;";
        _cmd.Parameters.AddWithValue("@correo", cliente.Correo);

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

        //Si se encontró, retornar cierto
        if (ds.Tables[0].Rows.Count > 0) {
          return true;
        }

        //***Identificar si existe por id***

        //Si no se pasó un id, retornar falso
        if (cliente.Id == 0) {
          return false;
        }

        //Limpiar parametros de anteriores querys
        _cmd.Parameters.Clear();

        //Verificar si existe por correo
        _cmd.CommandType = System.Data.CommandType.Text;
        _cmd.CommandText = "SELECT correo FROM cliente WHERE id = @id;";
        _cmd.Parameters.AddWithValue("@id", cliente.Id);

        //Abrir conexión
        _cmd.Connection.Open();

        //Dataset y adapter
        ds = new DataSet();
        adapter = new NpgsqlDataAdapter();

        //Ejecutar query
        adapter.SelectCommand = _cmd;

        //Rellenar ds con datos
        adapter.Fill(ds);

        //Cerrar Conexión
        _cmd.Connection.Close();

        //Si se encontró un registro, retornar
        if (ds.Tables[0].Rows.Count > 0) {
          return true;
        }

        //No se encontró
        return false;
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        return false;
      } finally {
        _cmd.Connection.Close();
      }
    }

    public Cliente BuscarCliente(int cliente_id) {
      //Limpiar parametros de anteriores querys
      _cmd.Parameters.Clear();

      //Construir sentencia
      _cmd.CommandType = System.Data.CommandType.Text;
      _cmd.CommandText = "SELECT id, nombre, apellido, telefono, correo, direccion FROM cliente WHERE id = @cliente_id";
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
      cliente.Id = Convert.ToInt32(ds.Tables[0].Rows[0]["id"].ToString());
      cliente.Nombre = ds.Tables[0].Rows[0]["nombre"].ToString();
      cliente.Apellido = ds.Tables[0].Rows[0]["apellido"].ToString();
      cliente.Telefono = ds.Tables[0].Rows[0]["telefono"].ToString();
      cliente.Correo = ds.Tables[0].Rows[0]["correo"].ToString();
      cliente.Direccion = ds.Tables[0].Rows[0]["direccion"].ToString();

      return cliente;
    }

    //Busca un usuario por id
    public Usuario BuscarUsuario(int usuario_id) {
      //Limpiar parametros de anteriores querys
      _cmd.Parameters.Clear();

      //Construir sentencia
      _cmd.CommandType = System.Data.CommandType.Text;
      _cmd.CommandText = "SELECT id, usuario FROM usuario WHERE id = @usuario_id";
      _cmd.Parameters.AddWithValue("@usuario_id", usuario_id);

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

      //Crear usuario a devolver
      Usuario usuario = new Usuario();

      //No se encontró usuario
      if (ds.Tables[0].Rows.Count <= 0) {
        return null;
      }

      //Asignar datos a usuario
      usuario.User = ds.Tables[0].Rows[0]["usuario"].ToString();
      usuario.Id = Convert.ToInt32(ds.Tables[0].Rows[0]["id"].ToString());

      return usuario;
    }

    //***ARTÍCULOS***//

    //Obtiene todos los artículos
    public List<Articulo> ObtenerArticulos() {
      List<Articulo> listaArticulos = new List<Articulo>();

      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, nombre, descripcion, precio, stock, paga_itbms FROM articulo;";

        //Si no hay una conexión abierta, abrirla
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Iniciarlizar dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        //Por cada fila en dataset, añadir un artículo a la lista
        foreach (DataRow row in ds.Tables[0].Rows) {
          listaArticulos.Add(new Articulo {
            Id = Convert.ToInt32(row["id"]),
            Nombre = row["nombre"].ToString()!,
            Descripcion = row["descripcion"].ToString()!,
            Precio = float.Parse(row["precio"].ToString()!),
            Stock = Convert.ToInt32(row["stock"]),
            Paga_itbms = Convert.ToBoolean(row["paga_itbms"])
          });
        }

        return listaArticulos;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticulos: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Obtener Artículo por ID
    public Articulo ObtenerArticuloPorId(int id) {
      try {
        //Limpiar parametros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, nombre, descripcion, precio, stock, paga_itbms " +
            "FROM articulo WHERE id = @id;";
        _cmd.Parameters.AddWithValue("@id", id);

        //Identificar si ya existe una conexión abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Declarar ds y adaptador
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar query
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        //Si no existe ningún producto con ese id, devolver null
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //Devolver un artículo
        return new Articulo {
          Id = Convert.ToInt32(row["id"]),
          Nombre = row["nombre"].ToString(),
          Descripcion = row["descripcion"].ToString(),
          Precio = float.Parse(row["precio"].ToString()!),
          Stock = Convert.ToInt32(row["stock"]),
          Paga_itbms = Convert.ToBoolean(row["paga_itbms"])
        };

      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticuloPorId: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Buscar Artículo por Nombre
    public List<Articulo> BuscarArticulosPorNombre(string nombreParcial) {
      List<Articulo> listaArticulos = new List<Articulo>();

      try {
        //Limpiar parámetros de queries anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, nombre, descripcion, precio, stock, paga_itbms " +
            "FROM articulo " +
            "WHERE LOWER(nombre) LIKE LOWER(@nombre) " +
            "ORDER BY nombre ASC;";

        //Parametro LIKE con %
        _cmd.Parameters.AddWithValue("@nombre", "%" + nombreParcial + "%");

        //Abrir conexión si es necesario
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        //Por cada fila en dataset, añadir un artículo
        foreach (DataRow row in ds.Tables[0].Rows) {
          listaArticulos.Add(new Articulo {
            Id = Convert.ToInt32(row["id"]),
            Nombre = row["nombre"].ToString()!,
            Descripcion = row["descripcion"].ToString()!,
            Precio = float.Parse(row["precio"].ToString()!),
            Stock = Convert.ToInt32(row["stock"]),
            Paga_itbms = Convert.ToBoolean(row["paga_itbms"])
          });
        }

        return listaArticulos;
      } catch (Exception ex) {
        Console.WriteLine("Error BuscarArticulosPorNombre: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Crear Artículo
    public Articulo CrearArticulo(Articulo articulo) {
      try {
        _cmd.Parameters.Clear();

        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "INSERT INTO articulo (nombre, descripcion, precio, stock, paga_itbms) " +
            "VALUES (@nombre, @descripcion, @precio, @stock, @itbms) RETURNING id;";

        _cmd.Parameters.AddWithValue("@nombre", articulo.Nombre);
        _cmd.Parameters.AddWithValue("@descripcion", articulo.Descripcion);
        _cmd.Parameters.AddWithValue("@precio", articulo.Precio);
        _cmd.Parameters.AddWithValue("@stock", articulo.Stock);
        _cmd.Parameters.AddWithValue("@itbms", articulo.Paga_itbms);

        //Si NO hay una conexión abierta, abrirla
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Obtener ID por separado, para debuggear con mayor facilidad
        var newId = _cmd.ExecuteScalar();
        articulo.Id = Convert.ToInt32(newId);

        //Retornar Artículo creado
        return articulo;
      } catch (Exception ex) {
        Console.WriteLine("Error Crear al crear un articulo: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Editar Artículo
    public Articulo EditarArticulo(Articulo articulo) {
      try {
        //Limpiar parametros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "UPDATE articulo SET nombre = @nombre, descripcion = @descripcion, precio = @precio, stock = @stock, paga_itbms = @itbms " +
            "WHERE id = @id";

        //Definir parametros
        _cmd.Parameters.AddWithValue("@id", articulo.Id);
        _cmd.Parameters.AddWithValue("@nombre", articulo.Nombre);
        _cmd.Parameters.AddWithValue("@descripcion", articulo.Descripcion);
        _cmd.Parameters.AddWithValue("@precio", articulo.Precio);
        _cmd.Parameters.AddWithValue("@stock", articulo.Stock);
        _cmd.Parameters.AddWithValue("@itbms", articulo.Paga_itbms);

        //Identificar si ya existe una conexión abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Obtener filas
        int rows = _cmd.ExecuteNonQuery();

        //Si se devolvió más de 0 filas, osea, se updateo el artículo: Devolver el mismo artículo
        //De lo contrario, devolver null
        return rows > 0 ? articulo : null;
      } catch (Exception ex) {
        Console.WriteLine("Error EditarArticulo: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Eliminar Artículo
    public bool EliminarArticulo(int id) {
      try {
        //Limpiar parametros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar delete statement
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "DELETE FROM articulo WHERE id = @id";
        _cmd.Parameters.AddWithValue("@id", id);

        //Si no existe uan conexión abierta, abrirla
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Obtenr filas
        int rows = _cmd.ExecuteNonQuery();

        //Devolver "cierto" si las filas afectadas son mayores a 0
        return rows > 0;
      } catch (Exception ex) {
        Console.WriteLine("Error EliminarArticulo: " + ex.Message);
        return false;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }


    //***CATEGORÍAS***//

    //Obtener TODAS las categorías
    public List<Categoria> ObtenerCategorias() {
      List<Categoria> listaCategorias = new List<Categoria>();

      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, nombre, categoria_padre_id FROM categoria;";

        //Abrir conexión si no está abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        //Cerrar Conexión
        _cmd.Connection.Close();

        //Por cada fila en dataset, añadir un artículo a la lista
        foreach (DataRow row in ds.Tables[0].Rows) {
          string id = row["id"].ToString();
          string name = row["nombre"].ToString()!;
          string padreId = row["categoria_padre_id"].ToString();

          //Si la categoría padre no existe
          if (string.IsNullOrEmpty(padreId)) {
            //Asignar null
            listaCategorias.Add(new Categoria {
              Id = Convert.ToInt32(id),
              Nombre = name,
              CategoriaPadreId = null,
            });

            //Asignar categorías padre si tiene
          } else {
            listaCategorias.Add(new Categoria {
              Id = Convert.ToInt32(id),
              Nombre = name,
              CategoriaPadreId = Convert.ToInt32(padreId),
            });

          }
        }

        return listaCategorias;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticulos: " + ex.Message);
        return null;
      } finally {
        //Si la conexión no fue cerrada, cerrar
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }
    
 

    //***FACTURAS***//

    //Obtener Facturas
    public List<Factura> ObtenerFacturas() {
      List<Factura> listaFacturas = new List<Factura>();

      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, cupon_id, subtotal, total, fecha, itbms, usuario_id FROM factura;";

        //Abrir conexión si no está abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        _cmd.Connection.Close();

        //Por cada fila, añadir una factura
        foreach (DataRow row in ds.Tables[0].Rows) {
          listaFacturas.Add(new Factura {
            Id = Convert.ToInt32(row["id"]),
            CuponId = row["cupon_id"] == DBNull.Value ? null : Convert.ToInt32(row["cupon_id"]),
            Subtotal = float.Parse(row["subtotal"].ToString()!),
            Total = float.Parse(row["total"].ToString()!),
            Fecha = Convert.ToDateTime(row["fecha"]),
            Itbms = float.Parse(row["itbms"].ToString()!),
            UsuarioId = Convert.ToInt32(row["usuario_id"])
          });
        }

        return listaFacturas;
      } catch (Exception ex) {
        Console.WriteLine("Error al obtener facturas: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

  }
}
