# Análisis del Proyecto WPF Gotyeam

Este documento proporciona una descripción detallada de la función de cada archivo en el proyecto WPF y un análisis sobre el nivel de cumplimiento de los requisitos establecidos en el enunciado del PDF.

## 1. Estructura y Funcionamiento de los Archivos

El proyecto sigue la arquitectura **MVVM (Model-View-ViewModel)**. A continuación se detalla la función de cada archivo agrupado por sus capas principales:

### Archivos Base y Estilos
* **`App.xaml` / `App.xaml.cs`**: Punto de entrada de la aplicación. Configura la carga inicial en `LoginView.xaml`, define las plantillas de datos (`DataTemplates`) globales para enlazar *ViewModels* a *Views*, importa los diccionarios de recursos globales, y gestiona de manera estática el reproductor de música de fondo (`BackgroundMusicPlayer`).
* **`AssemblyInfo.cs`**: Contiene la información y los metadatos a nivel de ensamblado para el manejo de los recursos y temas de la interfaz.
* **`Styles/UIColors.xaml`**: Diccionario de recursos que contiene toda la paleta de colores globales y pinceles visuales (`SolidColorBrush`) utilizados a lo largo de la aplicación.
* **`Styles/ButtonStyles.xaml`**: Diccionario que almacena las plantillas de control (`ControlTemplates`) para definir visualmente el diseño y los diferentes estados (normal, foco, hover) de los botones en la aplicación.

### Modelo (`MVVM/Model`)
Contiene las entidades que representan la capa de datos.
* **`JuegoModel.cs`**: Modelo de datos que representa un videojuego en la base de datos local, con sus atributos y notificaciones de cambio a la interfaz.
* **`LoginModel.cs`**: Define las clases `LoginRequestModel` y `LoginResponseModel` para estructurar los datos del usuario al enviar y recibir solicitudes a la API de autenticación.
* **`PerfilEmpleado.cs`**: Modelo de datos del empleado, controlando roles, estados activos e IDs, enlazado directamente a los campos editables.
* **`RawgModels.cs`**: Contiene múltiples clases diseñadas para serializar las respuestas JSON de la API externa de videojuegos RAWG (`RawgGameResult`, `RawgGame`, etc.).
* **`ResennaModel.cs`**: Representa las reseñas creadas por los usuarios en la plataforma.
* **`UserModel.cs`**: Modelo de datos para gestionar la información técnica y perfiles de los usuarios normales registrados.

### Vistas (`MVVM/View`)
Las vistas programadas en XAML que conforman la interfaz gráfica.
* **`MainView.xaml` / `.cs`**: Es la ventana "Dashboard" que hace de contenedor o "marco" de la aplicación. Incluye un menú lateral y un área de contenido en la que se inyectan dinámicamente el resto de `UserControls` al navegar.
* **`LoginView.xaml` / `.cs`**: Es la ventana inicial que se ejecuta donde el usuario o empleado ingresa sus credenciales por primera vez para acceder.
* **`GamesView.xaml` / `.cs` y `EmployeesView.xaml` / `.cs`**: Vistas encargadas de listar y mostrar pantallas modulares con la lista de videojuegos y de empleados, respectivamente.
* **`ProfileView.xaml` / `.cs`**: Pantalla para que el usuario actual visualice o edite la configuración de su perfil propio.
* **`UsersView.xaml` / `.cs`**: Interfaz similar a la de empleados pero adaptada para la lista de usuarios normales.
* **`SettingsView.xaml` / `.cs`**: Permite acceder a los parámetros generales de la configuración en tiempo real (tamaño de la ventana, volumen, toggle de música).
* **`AboutView.xaml` / `.cs`**: Vista puramente informativa que muestra datos crediticios de la aplicación y el desarrollador.
* **`Converters.cs`**: Contiene clases intermediarias que transforman unos datos en otros bajo la interfaz de `IValueConverter`. Ejemplos incluyen el `InverseBooleanConverter` (niega booleanos en XAML) y conversores que decodifican cadenas Base64 a imágenes.

### Controladores de Vista (`MVVM/ViewModel`)
El intermediario que se encarga de la lógica de presentación conectando los a las Vistas a los datos (Modelos).
* **`ViewModelBase.cs`**: Clase base obligatoria que implementa la interfaz `INotifyPropertyChanged` para la notificación reactiva a las vistas, y posee lógica base genérica como leer niveles de roles del sistema.
* **`MainViewModel.cs`**: Controla la navegación del Dashboard, definiendo un comando para cambiar la propiedad `CurrentView` a las diferentes vistas (`GamesVM`, `UsersVM`, etc.).
* **`LoginViewModel.cs`**: Utiliza `AuthService` para intentar conectar usando el correo provisto. Gestiona la autenticación de la `LoginView`.
* **`GamesViewModel.cs` / `EmployeesViewModel.cs` / `UsersViewModel.cs`**: Cada uno se comunica con sus respectivos "Servicios" (`GamesService`, `RawgService`, etc.) para importar colecciones iterables (`ObservableCollection`) de los modelos, preparándolos para ser mostrados en UI.
* **`SettingsViewModel.cs`**: Controla el volumen, música, persistencia a archivos JSON de la configuración, y el cambio del estilo de la resolución visual. 
* **`ProfileViewModel.cs` / `AboutViewModel.cs`**: Contienen la información de contexto de esas dos interfaces estáticas.
* **`RelayCommand.cs`**: (Ubicado en la raíz). Es el patrón de diseño indispensable para asociar y ejecutar botones de las Vistas XAML a funciones del ViewModel.

### Servicios (`Services`)
Clases dedicadas a establecer peticiones y la obtención de datos mediante el protocolo HTTP.
* **`AuthService.cs`**: Verifica credenciales en un endpoint de inicio de sesión de la API remota.
* **`EmployeesService.cs`, `GamesService.cs`, `UsersService.cs`**: Clases especializadas usando el objeto `HttpClient` que proveen rutinas de obtención y CRUD con con el backend en `http://localhost:5000`.
* **`GetEmployeeProfile.cs`**: Emplea el Token guardado estáticamente temporal para recuperar el perfil logeado del empleado.
* **`RawgService.cs`**: Usa una Key remota a `api.rawg.io` para acceder al repositorio y base de datos masivo mundial de videojuegos.


---

## 2. Requisitos del Enunciado (PDF) - Evaluación de Cumplimiento

Al revisar tu proyecto WPF frente a las exigencias descritas en el PDF del **PROYECTO FINAL 2025-2026**, aquí están los requisitos agrupados por los que tienes logrados y los que te restan por acometer o solucionar.

### ✅ REQUISITOS CUMPLIDOS

1.  **Arquitectura MVVM**: El requerimiento central para la calificación de (4 / 4 Estrellas) **está cumplido de manera rotunda**. Has separado cuidadosamente el código en subcarpetas de Model-View-ViewModel, controlando los estados y eventos a través de Bindings y el `MainViewModel`.
2.  **Módulo de Autenticación**: Tienes un Login central y un flujo de comprobación asincrónica contra la base de datos a través de credenciales enmascaradas.
3.  **Dashboard de Navegación**: Cumples sin problemas en el uso de la técnica Single-Page-Application dentro del marco central usando DataTemplates referidos en el `App.xaml` desde la clase `MainView`.
4.  **Menú y Acerca Del Proyecto**: Cumples con el área de AboutView y el selector de navegación de secciones básicas.
5.  **Configuraciones (`Settings`) y Ficheros Planos**: En el apartado SettingsView tienes cumplido el cambio de resolución y la manipulación de audio. Destacable que en el modelo implementas el requerimiento de nivel Intermedio empleando un archivo `.json` para persistir la configuración de la ventana del usuario.
6.  **UI/UX (Styling & Async)**: Se han implementado diccionarios de recursos compartidos (`UIColors.xaml`, `ButtonStyles.xaml`) y `ControlTemplates`. Además, los métodos de obtención en tus vistas utilizan los operadores `async` / `await` correctamente.

### ❌ REQUISITOS NO CUMPLIDOS O FALTANTES

1. **Botón de Auto-Reproducción e Hilos:**
   * La instrucción técnica pide un hilo persistente en segundo plano (`Task.Delay` o un hilo asíncrono) que ejecute ciclos de simulación sin congelar el Thread de la UI sin interacción del usuario.
2. **Selector de Nivel de Complejidad en Settings:**
   * Aunque tienes un módulo `Settings`, en el mandato se pide un seleccionador de complejidad de uso de la app (Por ej: "Modo Vista", "Modo Edición", "Modo Administrador"). La app debe capar funciones de edición dependiendo de lo elegido, o derivarlo según el Login que parezca poseer Roles. (De hecho dispones de roles en `PerfilEmpleado`, lo cual puede validar este tema, pero no hay un combobox expreso de Nivel de Edición en Settings según el PDF).
3. **Comentarios Estilo XML:**
   * El código C# no está excesivamente documentado usando etiquetas `/// <summary>`. Aunque sí empleas ocasionalmente este estilo como arriba del  `BackgroundMusicPlayer`, tendrías que extenderlo a las propiedades y funciones del modelo de vista por todos los archivos para considerarse un desarrollo de "máxima calificación".
