<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="assets/WL-wordmark-light.svg">
    <img src="assets/WL-wordmark.svg" alt="Who's Listening?" width="420">
  </picture>
</p>

# Who's Listening?

[Español](#español) · [English](#english)

<!-- captura: docs/captura.png -->

---

## Español

Un selector de cuentas para Spotify de escritorio en Windows.

No afiliado con Spotify.

### Qué hace

- Cada vez que se abre Spotify aparece la ventana "¿Quién está escuchando?" para elegir con qué cuenta escuchar.
- Queda como un ícono en la bandeja del sistema (junto al reloj), así se ve que está corriendo y se puede cerrar desde ahí.
- También se puede abrir cuando quieras desde el menú Inicio.
- Guarda, renombra y elimina cuentas.
- Las cuentas guardadas se cifran con la protección de Windows (DPAPI) para el usuario actual: copiadas a otro PC o a otro usuario no sirven.
- Está en 5 idiomas (español, inglés, portugués, francés y alemán) y sigue el idioma de Windows. Si Windows está en otro idioma, se usa inglés.
- Compatible con Spicetify, porque no toca los archivos del programa Spotify: solo modifica su archivo de configuración `prefs` y borra sus cachés de sesión.

### Requisitos

- Windows 10 u 11.
- Spotify de escritorio descargado desde [spotify.com](https://www.spotify.com/download/). **La versión de la Microsoft Store no es compatible.**
- .NET Framework 4.8, que ya viene incluido en Windows 10 y 11.

### Instalación

1. Descarga `WhosListening.exe` desde la página de **Releases** de este repositorio.
2. Ábrelo y acepta "¿Instalar Who's Listening? en este PC?".

**Si aparece "Windows protegió tu PC" (SmartScreen):** haz clic en "Más información" y después en "Ejecutar de todas formas". El aviso aparece porque el exe no tiene firma digital (code signing), no porque tenga algo malo.

**Antivirus:** algunos antivirus (por ejemplo Avast) pueden revisar durante unos segundos un exe nuevo y desconocido la primera vez que se abre. Después lo dejan correr; no hace falta agregar excepciones.

### Uso

- Abre Spotify y elige una cuenta en la ventana.
- **Agregar cuenta:**
  - "Guardar la cuenta actual": guarda la cuenta con la que Spotify tiene la sesión iniciada. Antes, inicia sesión en Spotify con "Recordarme" marcado.
  - "Preparar un inicio de sesión nuevo": abre Spotify en la pantalla de inicio de sesión para entrar con otra cuenta, que después puedes guardar.
- Al pasar el mouse sobre una cuenta aparecen los botones para cambiarle el nombre o eliminarla.
- Teclas **1–9** para elegir una cuenta y **Esc** para cerrar.
- El interruptor **"Mostrar al abrir Spotify"** activa o desactiva el selector automático. Apagado, el programa tampoco se inicia con Windows; lo abres desde el menú Inicio cuando lo necesites.
- Menú de la bandeja (clic derecho en el ícono): "Elegir cuenta", "Mostrar al abrir Spotify" y "Salir".
- **Diagnóstico** muestra información para reportar problemas. Es seguro pegarlo en un issue: los valores de las credenciales aparecen recortados a sus primeros 8 caracteres.

### Si venías del Spotify-Account-Switcher original

Si usabas [Spotify-Account-Switcher](https://github.com/Alpacinator/Spotify-Account-Switcher), la primera vez que abras Who's Listening? te ofrecerá importar las cuentas que tenías guardadas. La carpeta original no se borra.

### Desinstalar

Configuración → Aplicaciones → Aplicaciones instaladas → Who's Listening? → Desinstalar. Te pregunta si también quieres borrar las cuentas guardadas.

Carpetas que usa:

- `%LOCALAPPDATA%\Programs\WhosListening` (el programa)
- `%APPDATA%\WhosListening` (cuentas, configuración y registro)

### Cómo funciona

Para cambiar de cuenta, cierra Spotify, reemplaza las cuatro líneas `autologin.*` de `%APPDATA%\Spotify\prefs` por las de la cuenta elegida, borra la caché de sesión de Spotify (`dbrts`) y la carpeta de caché de esa cuenta, y vuelve a abrir Spotify. No envía nada a internet.

### Compilar desde el código

- Doble clic en `build.bat`. Usa el `csc.exe` que viene con .NET Framework 4.8, así que no hay que instalar nada. El resultado queda en `out\WhosListening.exe`.
- `build-test.bat` compila y corre las pruebas; el resultado queda en `out\tests.log`.

### Créditos

Inspirado en [Spotify-Account-Switcher](https://github.com/Alpacinator/Spotify-Account-Switcher) de Alpacinator. Este proyecto no contiene su código.

### Licencia

MIT. Ver [LICENSE](LICENSE).

---

## English

An account picker for the Spotify desktop app on Windows.

Not affiliated with Spotify.

### What it does

- Every time Spotify opens, the "Who's listening?" window shows up so you can choose which account to listen with.
- It stays as an icon in the system tray (next to the clock), so you can see it's running and close it from there.
- You can also open it any time from the Start menu.
- Saves, renames and deletes accounts.
- Saved accounts are encrypted with Windows protection (DPAPI) for the current user: copied to another PC or another user, they don't work.
- Available in 5 languages (Spanish, English, Portuguese, French and German), following the Windows language. If Windows uses another language, English is used.
- Compatible with Spicetify, because it doesn't touch Spotify's program files: it only edits its `prefs` settings file and clears its session caches.

### Requirements

- Windows 10 or 11.
- The Spotify desktop app downloaded from [spotify.com](https://www.spotify.com/download/). **The Microsoft Store version is not supported.**
- .NET Framework 4.8, already included in Windows 10 and 11.

### Installation

1. Download `WhosListening.exe` from this repository's **Releases** page.
2. Open it and accept "Install Who's Listening? on this PC?".

**If you see "Windows protected your PC" (SmartScreen):** click "More info" and then "Run anyway". The warning appears because the exe isn't code-signed, not because something is wrong with it.

**Antivirus:** some antivirus programs (for example Avast) may scan a new, unknown exe for a few seconds the first time it's opened. After that they let it run; no exceptions are needed.

### Usage

- Open Spotify and pick an account in the window.
- **Add account:**
  - "Save the current account": saves the account Spotify is currently signed in with. Sign in to Spotify with "Remember me" checked first.
  - "Prepare a new sign-in": opens Spotify on the sign-in screen so you can log in with another account, which you can then save.
- Hovering over an account shows the buttons to rename or delete it.
- Keys **1–9** pick an account and **Esc** closes the window.
- The **"Show when Spotify opens"** switch turns the automatic picker on or off. When it's off, the program also doesn't start with Windows; open it from the Start menu when you need it.
- Tray menu (right-click the icon): "Choose account", "Show when Spotify opens" and "Exit".
- **Diagnostics** shows information for reporting problems. It's safe to paste into an issue: credential values are truncated to their first 8 characters.

### Coming from the original Spotify-Account-Switcher

If you used [Spotify-Account-Switcher](https://github.com/Alpacinator/Spotify-Account-Switcher), the first time you open Who's Listening? it will offer to import the accounts you had saved. The original folder is not deleted.

### Uninstall

Settings → Apps → Installed apps → Who's Listening? → Uninstall. It asks whether you also want to delete the saved accounts.

Folders it uses:

- `%LOCALAPPDATA%\Programs\WhosListening` (the program)
- `%APPDATA%\WhosListening` (accounts, settings and log)

### How it works

To switch accounts, it closes Spotify, replaces the four `autologin.*` lines in `%APPDATA%\Spotify\prefs` with the chosen account's, clears Spotify's session cache (`dbrts`) and that account's cache folder, and opens Spotify again. Nothing is sent to the internet.

### Building from source

- Double-click `build.bat`. It uses the `csc.exe` included with .NET Framework 4.8, so there's nothing to install. The output is `out\WhosListening.exe`.
- `build-test.bat` builds and runs the tests; the result is written to `out\tests.log`.

### Credits

Inspired by [Spotify-Account-Switcher](https://github.com/Alpacinator/Spotify-Account-Switcher) by Alpacinator. This project contains none of its code.

### License

MIT. See [LICENSE](LICENSE).
