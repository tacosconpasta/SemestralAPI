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
        //Limpiar parámetros de querys anteriores
        _cmd.Parameters.Clear();

        //Preparar query
        _cmd.CommandType = CommandType.Text;
        _cmd.CommandText = "SELECT id, nombre, categoria_padre_id FROM categoria;";

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

  }
}
