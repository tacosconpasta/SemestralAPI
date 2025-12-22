using Npgsql;
using SemestralAPI.Models;
using System.Data;
using System.Xml.Linq;

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

    //***USUARIO-CLIENTE***//

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

    //Buscar usuario de cliente mediante cliente_id
    public Usuario BuscarUsuarioByClienteId(int cliente_id) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
              SELECT id, usuario, contrasena, rol, cliente_id
              FROM usuario
              WHERE cliente_id = @cliente_id
                AND rol = 'cliente'
              LIMIT 1;
        ";

        //Asignar parámetros
        _cmd.Parameters.AddWithValue("@cliente_id", cliente_id);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar DataSet y Adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;

        //Ejecutar query
        adapter.Fill(ds);

        //Si no hay resultados, retornar null
        if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //Construir objeto Usuario
        Usuario usuario = new Usuario {
          Id = Convert.ToInt32(row["id"]),
          User = row["usuario"].ToString(),
          Contrasena = row["contrasena"].ToString(),
          Rol = row["rol"].ToString(),
          ClienteId = row["cliente_id"] == DBNull.Value
                        ? null
                        : Convert.ToInt32(row["cliente_id"])
        };

        return usuario;

      } catch (Exception ex) {
        Console.WriteLine("Error BuscarUsuario: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
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
        _cmd.CommandText = "SELECT id, nombre, descripcion, precio, stock, paga_itbms FROM articulo ORDER BY id;";

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

    //Filtrar artículos pertenecientes a categoría y a hijos de esta
    public List<dynamic> ObtenerArticulosPorCategoriaRecursiva(int categoriaId) {
      List<dynamic> articulos = new List<dynamic>();

      try {
        //Limpiar parametros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar Query recurisva
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
              WITH RECURSIVE categorias_filtradas AS (
                SELECT id
                FROM categoria
                WHERE id = @categoria_id

                UNION ALL

                SELECT c.id
                FROM categoria c
                INNER JOIN categorias_filtradas cf
                  ON c.categoria_padre_id = cf.id
              )
              SELECT
                a.id AS articulo_id,
                a.nombre AS articulo_nombre,
                a.descripcion,
                a.precio,
                a.stock,
                a.paga_itbms,
                c.id AS categoria_id,
                c.nombre AS categoria_nombre
              FROM articulo a
              INNER JOIN articulo_categoria ac ON ac.id_articulo = a.id
              INNER JOIN categoria c ON c.id = ac.id_categoria
              WHERE a.id IN (
                SELECT ac2.id_articulo
                FROM articulo_categoria ac2
                WHERE ac2.id_categoria IN (SELECT id FROM categorias_filtradas)
              )
              ORDER BY a.nombre, c.nombre;
            ";
        _cmd.Parameters.AddWithValue("@categoria_id", categoriaId);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y rellenar adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Mapear agrupando por artículo
        foreach (DataRow row in ds.Tables[0].Rows) {
          int articuloId = Convert.ToInt32(row["articulo_id"]);

          var articulo = articulos.FirstOrDefault(a => a.Id == articuloId);

          if (articulo == null) {
            articulo = new {
              Id = articuloId,
              Nombre = row["articulo_nombre"].ToString(),
              Descripcion = row["descripcion"].ToString(),
              Precio = Convert.ToDecimal(row["precio"]),
              Stock = Convert.ToInt32(row["stock"]),
              PagaITBMS = Convert.ToBoolean(row["paga_itbms"]),
              Categorias = new List<object>()
            };
            articulos.Add(articulo);
          }

          ((List<object>) articulo.Categorias).Add(new {
            Id = Convert.ToInt32(row["categoria_id"]),
            Nombre = row["categoria_nombre"].ToString()
          });
        }

        return articulos;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticulosPorCategoriaRecursiva: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Obtener categorías de un Artículo
    public List<Categoria> ObtenerCategoriasPorArticulo(int articuloId) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
            SELECT c.id, c.nombre, c.categoria_padre_id
            FROM articulo_categoria ac
            INNER JOIN categoria c ON c.id = ac.id_categoria
            WHERE ac.id_articulo = @articulo_id
            ORDER BY c.nombre;
         ";
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y adapter y rellenarlo
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Preparar lista a devolver
        List<Categoria> categorias = new List<Categoria>();

        //Si no hay resultados, devolver lista vacía
        if (ds.Tables.Count == 0 || ds.Tables[0].Rows.Count == 0)
          return categorias;

        //Mapear resultados
        foreach (DataRow row in ds.Tables[0].Rows) {
          categorias.Add(new Categoria {
            Id = Convert.ToInt32(row["id"]),
            Nombre = row["nombre"].ToString(),
            CategoriaPadreId = row["categoria_padre_id"] == DBNull.Value
                                ? (int?) null
                                : Convert.ToInt32(row["categoria_padre_id"])
          });
        }

        return categorias;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerCategoriasPorArticulo: " + ex.Message);
        return null;
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

    //Obtener UNA categoría por ID 
    public Categoria ObtenerCategoria(int categoriaABuscarId) {
      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, nombre, categoria_padre_id FROM categoria WHERE id = @id;";
        _cmd.Parameters.AddWithValue("@id", categoriaABuscarId);

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

        //Si no existe ninguna categoría con ese id, devolver null
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];
        string id = row["id"].ToString();
        string name = row["nombre"].ToString()!;
        string padreId = row["categoria_padre_id"].ToString();

        //Si la categoría padre no existe
        if (string.IsNullOrEmpty(padreId)) {
          //Devolver la categoria con categoriaPadre = null
          return new Categoria {
            Id = Convert.ToInt32(id),
            Nombre = name,
            CategoriaPadreId = null,
          };
        } else {
          //Devolver la categoria con categoriaPadre
          return new Categoria {
            Id = Convert.ToInt32(id),
            Nombre = name,
            CategoriaPadreId = Convert.ToInt32(padreId),
          };
        }
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticulos: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Obtener UNA categoría por UN nombre
    public Categoria ObtenerCategoria(string nombre) {
      List<Categoria> listaCategorias = new List<Categoria>();

      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, nombre, categoria_padre_id FROM categoria WHERE nombre ILIKE @nombre;";
        _cmd.Parameters.AddWithValue("@nombre", nombre);

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

        //Si no existe ninguna categoría con ese id, devolver null
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];
        string id = row["id"].ToString();
        string name = row["nombre"].ToString()!;
        string padreId = row["categoria_padre_id"].ToString();

        //Si la categoría padre no existe
        if (string.IsNullOrEmpty(padreId)) {
          //Devolver la categoria con categoriaPadre = null
          return new Categoria {
            Id = Convert.ToInt32(id),
            Nombre = name,
            CategoriaPadreId = null,
          };
        } else {
          //Devolver la categoria con categoriaPadre
          return new Categoria {
            Id = Convert.ToInt32(id),
            Nombre = name,
            CategoriaPadreId = Convert.ToInt32(padreId),
          };
        }
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerArticulos: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Buscar VARIAS categorias con instancias nombre
    public List<Categoria> BuscarCategorias(string nombre) {
      try {
        //Limpiar parametros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, nombre, categoria_padre_id " +
            "FROM categoria " +
            "WHERE nombre ILIKE @nombre " +
            "ORDER BY nombre";

        _cmd.Parameters.AddWithValue("@nombre", "%" + nombre + "%");

        //Abrir conexión si no existe
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar adapter
        adapter.Fill(ds);

        //Inicializar lista de categorias que hicieron match con el parámetro de búsqueda
        List<Categoria> categorias = new List<Categoria>();

        //Por cada resultado
        foreach (DataRow row in ds.Tables[0].Rows) {
          //Añadirlo a lista de categorias encontradas
          categorias.Add(new Categoria {
            Id = Convert.ToInt32(row["id"]),
            Nombre = row["nombre"].ToString()!,
            CategoriaPadreId = row["categoria_padre_id"] == DBNull.Value
                ? 0
                : Convert.ToInt32(row["categoria_padre_id"])
          });
        }

        //Retornar categorías encontradas
        return categorias;
      } catch (Exception ex) {
        Console.WriteLine("Error BuscarCategorias: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }


    //Añadir una categoría
    public bool AgregarCategoria(string nombreCategoria, int categoriaPadre) {
      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
          INSERT INTO categoria (nombre, categoria_padre_id)
          VALUES (@nombre, @categoria_padre_id);
        ";

        //Asignar parámetros
        _cmd.Parameters.AddWithValue("@nombre", nombreCategoria);

        //Permitir NULL en categoria_padre_id
        if (categoriaPadre > 0)
          _cmd.Parameters.AddWithValue("@categoria_padre_id", categoriaPadre);
        else
          _cmd.Parameters.AddWithValue("@categoria_padre_id", DBNull.Value);

        //Si no hay una conexión abierta, abrirla
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Ejecutar comando
        _cmd.ExecuteNonQuery();

        return true;
      } catch (Exception ex) {
        Console.WriteLine("Error al añadir categoria: " + ex.Message);
        return false;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Eliminar una categoría
    public bool EliminarCategoria(int categoriaAEliminarId) {
      try {
        //Limpiar parametros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar delete statement
        _cmd.CommandType = CommandType.Text;

        //El campo id es ON CASCADE, elimina AUTOMÁTICAMENTE a TODOS los hijos con ese ID y sucesivamente
        _cmd.CommandText = "DELETE FROM categoria WHERE id = @id";
        _cmd.Parameters.AddWithValue("@id", categoriaAEliminarId);

        //Si no existe uan conexión abierta, abrirla
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Obtener filas
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

    //Editar Categoría
    public Categoria EditarCategoria(Categoria categoria) {
      try {
        //Limpiar parametros anteriores
        _cmd.Parameters.Clear();

        //Preparar query base
        _cmd.CommandType = CommandType.Text;

        if (categoria.CategoriaPadreId == -1) {
          //Eliminar categoría padre
          _cmd.CommandText =
              "UPDATE categoria SET nombre = @nombre, categoria_padre_id = NULL " +
              "WHERE id = @id";
        } else if (categoria.CategoriaPadreId > 0) {
          //Actualizar categoría padre
          _cmd.CommandText =
              "UPDATE categoria SET nombre = @nombre, categoria_padre_id = @categoria_padre_id " +
              "WHERE id = @id";

          _cmd.Parameters.AddWithValue("@categoria_padre_id", categoria.CategoriaPadreId);
        } else {
          //No tocar categoría padre
          _cmd.CommandText =
              "UPDATE categoria SET nombre = @nombre " +
              "WHERE id = @id";
        }

        //Parámetros comunes
        _cmd.Parameters.AddWithValue("@id", categoria.Id);
        _cmd.Parameters.AddWithValue("@nombre", categoria.Nombre);

        //Identificar si ya existe una conexión abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Ejecutar update
        int rows = _cmd.ExecuteNonQuery();

        return rows > 0 ? categoria : null;
      } catch (Exception ex) {
        Console.WriteLine("Error EditarCategoria: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Obtener Categoría con hijos (Recursivo)
    public CategoriaArbol ObtenerCategoriaArbol(int categoriaId) {
      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Construir Query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
            WITH RECURSIVE categorias_arbol AS (
              SELECT id, nombre, categoria_padre_id
               FROM categoria
              WHERE id = @id

              UNION ALL

              SELECT c.id, c.nombre, c.categoria_padre_id
              FROM categoria c
              INNER JOIN categorias_arbol ca
                ON c.categoria_padre_id = ca.id
              )
            SELECT id, nombre, categoria_padre_id
            FROM categorias_arbol;
          ";
        _cmd.Parameters.AddWithValue("@id", categoriaId);

        //Abir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();

        //Ejecutar comando
        adapter.SelectCommand = _cmd;

        //Rellenar dataset
        adapter.Fill(ds);

        //Si no se seleccionó nada
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        //Crear diccionario para armar el árbol
        Dictionary<int, CategoriaArbol> categorias = new();

        foreach (DataRow row in ds.Tables[0].Rows) {
          int id = Convert.ToInt32(row["id"]);

          categorias[id] = new CategoriaArbol {
            Id = id,
            Nombre = row["nombre"].ToString()!
          };
        }

        CategoriaArbol raiz = null;

        foreach (DataRow row in ds.Tables[0].Rows) {
          int id = Convert.ToInt32(row["id"]);
          object padreObj = row["categoria_padre_id"];

          //Si la categoria actual no tiene padre
          if (padreObj == DBNull.Value) {
            raiz = categorias[id];

            //Si tiene padre
          } else {
            //Parsear
            int padreId = Convert.ToInt32(padreObj.ToString());

            if (categorias.ContainsKey(padreId))
              categorias[padreId].Hijos.Add(categorias[id]);
          }
        }

        return raiz;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerCategoriaArbol: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }




    //***CUPONES***//
    public List<Cupon> ObtenerCupones() {
      List<Cupon> lista = new List<Cupon>();

      try {
        _cmd.Parameters.Clear();

        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, codigo, descuento, estado FROM cupon ORDER BY id";

        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        foreach (DataRow row in ds.Tables[0].Rows) {
          lista.Add(new Cupon {
            Id = Convert.ToInt32(row["id"]),
            Codigo = row["codigo"].ToString()!,
            Descuento = Convert.ToDecimal(row["descuento"]),
            Estado = Convert.ToBoolean(row["estado"])
          });
        }

        return lista;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerCupones: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    public Cupon ObtenerCuponPorCodigo(string codigo) {
      try {
        //Limpiar parametros de querys anteriores
        _cmd.Parameters.Clear();

        //Contruir query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, codigo, descuento, estado " +
            "FROM cupon WHERE codigo = @codigo";

        _cmd.Parameters.AddWithValue("@codigo", codigo);

        //Abrir conexion si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Iniicalizar dataset y ejecutar consulta
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Si no se encontró ninguno, retornar null
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //De lo contrario, devolver el encontrado
        return new Cupon {
          Id = Convert.ToInt32(row["id"]),
          Codigo = row["codigo"].ToString()!,
          Descuento = Convert.ToDecimal(row["descuento"]),
          Estado = Convert.ToBoolean(row["estado"])
        };
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerCuponPorCodigo: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    public Cupon BuscarCuponPorCodigo(string codigo) {
      try {
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //COnstruir query para obtener id de cupon en base a codigo de cupon
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
            SELECT id, codigo, descuento, estado
            FROM cupon
            WHERE codigo = @codigo
            LIMIT 1;
          ";
        _cmd.Parameters.AddWithValue("@codigo", codigo);

        //Abrir conexión, si cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //DataSet y adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter {
          SelectCommand = _cmd
        };

        //Rellenar adapter
        adapter.Fill(ds);

        //Verificar filas
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //Retornar cupon si existe
        return new Cupon {
          Id = Convert.ToInt32(row["id"]),
          Codigo = row["codigo"].ToString(),
          Descuento = Convert.ToDecimal(row["descuento"]),
          Estado = Convert.ToBoolean(row["estado"])
        };

      } catch (Exception ex) {
        Console.WriteLine("Error BuscarCuponPorCodigo: " + ex.Message);
        return null;
      } finally {
        _cmd.Connection.Close();
      }
    }

    public bool AplicarCuponAOrden(int ordenId, int? cuponId) {
      try {
        _cmd.Parameters.Clear();

        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
            UPDATE orden
            SET cupon_id = @cupon_id
            WHERE id = @orden_id;
        ";

        _cmd.Parameters.AddWithValue("@orden_id", ordenId);

        if (cuponId.HasValue)
          _cmd.Parameters.AddWithValue("@cupon_id", cuponId.Value);
        else
          _cmd.Parameters.AddWithValue("@cupon_id", DBNull.Value);

        _cmd.Connection.Open();

        return _cmd.ExecuteNonQuery() > 0;

      } catch (Exception ex) {
        Console.WriteLine("Error AplicarCuponAOrden: " + ex.Message);
        return false;
      } finally {
        _cmd.Connection.Close();
      }
    }




    //***ORDENES***//

    //Obtener una orden por su Id
    public Orden ObtenerOrdenPorId(int ordenId) {
      try {
        //Limpiar parametros de Querys anteriores
        _cmd.Parameters.Clear();

        //Construir Query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
          SELECT
              id,
            estado,
            usuario_id,
            cupon_id,
            subtotal,
            total,
            descuento,
            itbms
          FROM orden
          WHERE id = @id
          LIMIT 1;
        ";
        _cmd.Parameters.AddWithValue("@id", ordenId);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter {
          SelectCommand = _cmd
        };

        adapter.Fill(ds);

        //identificar si se recogió alguna orden
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //Si existe una orden, retornarla
        return new Orden {
          Id = Convert.ToInt32(row["id"]),
          Estado = row["estado"].ToString(),
          Usuario_Id = Convert.ToInt32(row["usuario_id"]),
          Cupon_Id = row["cupon_id"] == DBNull.Value ? null : (int?) Convert.ToInt32(row["cupon_id"]),
          Subtotal = Convert.ToDecimal(row["subtotal"]),
          Total = Convert.ToDecimal(row["total"]),
          Descuento = row["descuento"] == DBNull.Value ? 0 : Convert.ToDecimal(row["descuento"]),
          Itbms = Convert.ToDecimal(row["itbms"])
        };

      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerOrdenPorId: " + ex.Message);
        return null;
      } finally {
        _cmd.Connection.Close();
      }
    }

    //Obtener INFORMACIÓN de Carrito (No el carrito)
    public Orden ObtenerOrdenEnProceso(int usuarioId) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Construir query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, estado, fecha, usuario_id, cupon_id, subtotal, total " +
            "FROM orden " +
            "WHERE usuario_id = @usuario_id AND estado = 'procesando'";

        //Definir parámetros
        _cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

        //Abrir conexión si no está abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Si no hay orden en proceso
        if (ds.Tables[0].Rows.Count == 0)
          return null;

        DataRow row = ds.Tables[0].Rows[0];

        //Retornar Orden en proceso
        return new Orden {
          Id = Convert.ToInt32(row["id"]),
          Estado = row["estado"].ToString()!,
          Fecha = Convert.ToDateTime(row["fecha"]),
          Usuario_Id = Convert.ToInt32(row["usuario_id"]),
          Cupon_Id = row["cupon_id"] == DBNull.Value
              ? (int?) null
              : Convert.ToInt32(row["cupon_id"]),
          Subtotal = Convert.ToDecimal(row["subtotal"]),
          Total = Convert.ToDecimal(row["total"])
        };

      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerOrdenEnProceso: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Iniciar Orden vacía, sólo se necesita usuario a asociar
    public int CrearOrden(int usuarioId) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "INSERT INTO orden (estado, fecha, usuario_id, subtotal, total) " +
            "VALUES ('procesando', NOW(), @usuario_id, 0, 0) " +
            "RETURNING id";

        //Definir parámetros
        _cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

        //Abrir conexión si no está abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Ejecutar y obtener id
        object result = _cmd.ExecuteScalar();

        //Si el resultado no es válido, osea, no se creo nada
        if (result == null)
          return -1;

        //Si el resultado es válido
        return Convert.ToInt32(result);
      } catch (Exception ex) {
        Console.WriteLine("Error CrearOrden: " + ex.Message);
        return -1;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Actualizar solo el estado de una orden
    public bool ActualizarEstadoOrden(int ordenId, string estado) {
      try {
        _cmd.Parameters.Clear();

        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
            UPDATE orden
            SET estado = @estado
            WHERE id = @id;
        ";

        _cmd.Parameters.AddWithValue("@id", ordenId);
        _cmd.Parameters.AddWithValue("@estado", estado);

        _cmd.Connection.Open();

        return _cmd.ExecuteNonQuery() > 0;

      } catch (Exception ex) {
        Console.WriteLine("Error ActualizarEstadoOrden: " + ex.Message);
        return false;
      } finally {
        _cmd.Connection.Close();
      }
    }

    //Finalizar una orden (proceso => revision) y crear factura
    public bool FinalizarOrden(int ordenId) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //1️⃣ Obtener datos de la orden
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = @"
      SELECT
        estado,
        subtotal,
        COALESCE(descuento, 0) AS descuento,
        total,
        itbms,
        usuario_id
      FROM orden
      WHERE id = @orden_id;
    ";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);

        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Si la orden no existe
        if (ds.Tables[0].Rows.Count == 0)
          return false;

        DataRow row = ds.Tables[0].Rows[0];

        //Validar estado
        string estadoActual = row["estado"].ToString();
        if (estadoActual != "procesando")
          throw new InvalidOperationException("ESTADO_INVALIDO");

        decimal subtotal = Convert.ToDecimal(row["subtotal"]);
        decimal descuento = Convert.ToDecimal(row["descuento"]);
        decimal total = Convert.ToDecimal(row["total"]);
        decimal itbms = Convert.ToDecimal(row["itbms"]);
        int usuarioId = Convert.ToInt32(row["usuario_id"]);

        //2️⃣ Cambiar estado de la orden
        _cmd.Parameters.Clear();
        _cmd.CommandText = @"
      UPDATE orden
      SET estado = 'completada'
      WHERE id = @orden_id;
    ";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);

        if (_cmd.ExecuteNonQuery() == 0)
          return false;

        //3️⃣ Crear factura
        _cmd.Parameters.Clear();
        _cmd.CommandText = @"
      INSERT INTO factura (
        subtotal,
        descuento,
        total,
        fecha,
        itbms,
        usuario_id
      )
      VALUES (
        @subtotal,
        @descuento,
        @total,
        NOW(),
        @itbms,
        @usuario_id
      );
    ";

        _cmd.Parameters.AddWithValue("@subtotal", subtotal);
        _cmd.Parameters.AddWithValue("@descuento", descuento);
        _cmd.Parameters.AddWithValue("@total", total);
        _cmd.Parameters.AddWithValue("@itbms", itbms);
        _cmd.Parameters.AddWithValue("@usuario_id", usuarioId);

        //Insertar factura
        int rows = _cmd.ExecuteNonQuery();

        return rows > 0;

      } catch (InvalidOperationException) {
        throw;

      } catch (Exception ex) {
        Console.WriteLine("Error FinalizarOrden: " + ex.Message);
        return false;

      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }




    //***CARRITOS***//
    //Obtener el carrito en base a orden Id
    public List<Orden_Detalle> ObtenerDetallesOrden(int ordenId) {
      List<Orden_Detalle> lista = new List<Orden_Detalle>();

      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id, orden_id, articulo_id, cantidad, precio_final " +
            "FROM orden_detalle " +
            "WHERE orden_id = @orden_id " +
            "ORDER BY id";

        //Definir parámetros
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);

        //Abrir conexión si no está abierta
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Dataset
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Recorrer resultados
        foreach (DataRow row in ds.Tables[0].Rows) {
          lista.Add(new Orden_Detalle {
            Id = Convert.ToInt32(row["id"]),
            Orden_Id = Convert.ToInt32(row["orden_id"]),
            Articulo_Id = Convert.ToInt32(row["articulo_id"]),
            Cantidad = Convert.ToInt32(row["cantidad"]),
            Precio_Final = Convert.ToDecimal(row["precio_final"])
          });
        }

        return lista;
      } catch (Exception ex) {
        Console.WriteLine("Error ObtenerDetallesOrden: " + ex.Message);
        return null;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Agrega un artículo al carrito
    public bool AgregarArticuloOrden(int ordenId, int articuloId, int cantidad) {
      try {
        //Limpiar parámetros de query anterior
        _cmd.Parameters.Clear();
        _cmd.CommandType = CommandType.Text;

        //Obtener precio del artículo a insertar
        _cmd.CommandText = "SELECT precio FROM articulo WHERE id = @articulo_id";
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Abrir conexion si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Ejecutar query sobre precioObj
        object precioObj = _cmd.ExecuteScalar();
        if (precioObj == null)
          return false;

        //Convertir el precio obtenido en valor válido
        decimal precioFinal = Convert.ToDecimal(precioObj);
        precioFinal = precioFinal * cantidad;

        //Construir INSERT (dispara trigger)
        _cmd.Parameters.Clear();
        _cmd.CommandText = @"
          INSERT INTO orden_detalle (orden_id, articulo_id, cantidad, precio_final)
          VALUES (@orden_id, @articulo_id, @cantidad, @precio_final);
         ";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);
        _cmd.Parameters.AddWithValue("@cantidad", cantidad);
        _cmd.Parameters.AddWithValue("@precio_final", precioFinal);

        _cmd.ExecuteNonQuery();
        return true;
      } catch (PostgresException ex) {
        //Stock insuficiente (trigger)
        if (ex.SqlState == "P0002")
          throw;

        //Artículo ya existe en orden; sumar cantidad
        if (ex.SqlState == "23505") {
          _cmd.Parameters.Clear();
          _cmd.CommandText = @"
            UPDATE orden_detalle
            SET cantidad = cantidad + @cantidad,
            precio_final = precio_final + @precio_final
            WHERE orden_id = @orden_id AND articulo_id = @articulo_id;
          ";

          _cmd.Parameters.AddWithValue("@cantidad", cantidad);
          _cmd.Parameters.AddWithValue("@orden_id", ordenId);
          _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

          _cmd.ExecuteNonQuery();
          return true;
        }

        throw;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Eliminar artículo del carrito
    public bool EliminarArticuloOrden(int ordenId, int articuloId) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Verificar si existe la relación
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT id " +
            "FROM orden_detalle " +
            "WHERE orden_id = @orden_id AND articulo_id = @articulo_id";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y obtener datos con adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Si no existe la relación, retornar falso (no se eliminó nada)
        if (ds.Tables[0].Rows.Count == 0)
          return false;

        //Eliminar artículo de carrito
        _cmd.Parameters.Clear();
        _cmd.CommandText =
            "DELETE FROM orden_detalle " +
            "WHERE orden_id = @orden_id AND articulo_id = @articulo_id";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Ejecutar DELETE
        int rows = _cmd.ExecuteNonQuery();

        //Retornar true, si las filas del DELETE son 1 o más
        return rows > 0;
      } catch (Exception ex) {
        Console.WriteLine("Error EliminarArticuloOrden: " + ex.Message);
        return false;
      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }

    //Actualizar cantidad de un artículo en el carrito
    public bool ActualizarArticuloOrden(int ordenId, int articuloId, int nuevaCantidad) {
      try {
        //Limpiar parámetros anteriores
        _cmd.Parameters.Clear();

        //Query para veerificar si existe ya la relación en el carrito
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText =
            "SELECT cantidad " +
            "FROM orden_detalle " +
            "WHERE orden_id = @orden_id AND articulo_id = @articulo_id";
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Abrir conexión si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Inicializar dataset y obtener datos con adapter
        DataSet ds = new DataSet();
        NpgsqlDataAdapter adapter = new NpgsqlDataAdapter();
        adapter.SelectCommand = _cmd;
        adapter.Fill(ds);

        //Si no existe la relación, retornar falso (no se puede actualizar algo que no existe)
        if (ds.Tables[0].Rows.Count == 0)
          return false;

        //Actualizar la cantidad
        _cmd.Parameters.Clear();
        _cmd.CommandText =
            "UPDATE orden_detalle " +
            "SET cantidad = @cantidad " +
            "WHERE orden_id = @orden_id AND articulo_id = @articulo_id";
        _cmd.Parameters.AddWithValue("@cantidad", nuevaCantidad);
        _cmd.Parameters.AddWithValue("@orden_id", ordenId);
        _cmd.Parameters.AddWithValue("@articulo_id", articuloId);

        //Obtener filas modificadas
        int rows = _cmd.ExecuteNonQuery();

        //Retornar cierto si se modificó 1 o más filas
        return rows > 0;
      } catch (PostgresException ex) {
        //Errores del trigger
        if (ex.SqlState == "P0002") // STOCK_INSUFICIENTE
          throw;

        Console.WriteLine("Error ActualizarArticuloOrden: " + ex.Message);
        return false;

      } catch (Exception ex) {
        Console.WriteLine("Error ActualizarArticuloOrden: " + ex.Message);
        return false;

      } finally {
        if (_cmd.Connection.State != ConnectionState.Closed)
          _cmd.Connection.Close();
      }
    }


    //***FOTOS***//
    public Foto AgregarFoto(Foto fotoAInsertar) {
      try {
        //Limpiar parámetros de query anterior
        _cmd.Parameters.Clear();

        //Construir Query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "INSERT INTO foto VALUES(@foto, @articulo_id)";
        _cmd.Parameters.AddWithValue("@foto", fotoAInsertar.FotoPrincipal);
        _cmd.Parameters.AddWithValue("@articulo_id", fotoAInsertar.ArticuloId);

        //Abrir conexion si está cerrada
        if (_cmd.Connection.State != ConnectionState.Open)
          _cmd.Connection.Open();

        //Obtener ID por separado, para debuggear con mayor facilidad
        var newId = _cmd.ExecuteScalar();
        fotoAInsertar.Id = Convert.ToInt32(newId);

        return fotoAInsertar;
      } catch (Exception e) {
        Console.Error.WriteLine("Ocurrió un error AgregarFoto: " + e.Message);
        return null;

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
