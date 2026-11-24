using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhosListening
{
    public class SpotifyPrefs
    {
        public static readonly string[] AuthKeys = new string[]
        {
            "autologin.username",
            "autologin.canonical_username",
            "autologin.blob",
            "autologin.saved_credentials"
        };

        private List<string> lines = new List<string>();
        private string eol = "\n";
        private bool trailingNewline = true;

        public static SpotifyPrefs Load(string path)
        {
            SpotifyPrefs p = new SpotifyPrefs();
            if (!File.Exists(path)) return p;

            byte[] bytes = File.ReadAllBytes(path);
            int start = 0;
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) start = 3;
            string text = new UTF8Encoding(false).GetString(bytes, start, bytes.Length - start);
            if (text.Length == 0) return p;

            p.eol = text.Contains("\r\n") ? "\r\n" : "\n";
            p.trailingNewline = text.EndsWith("\n");
            string[] parts = text.Split('\n');
            int count = parts.Length;
            if (p.trailingNewline) count--;
            for (int i = 0; i < count; i++)
            {
                string line = parts[i];
                if (line.EndsWith("\r")) line = line.Substring(0, line.Length - 1);
                p.lines.Add(line);
            }
            return p;
        }

        public void Save(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) sb.Append(eol);
                sb.Append(lines[i]);
            }
            if (lines.Count > 0 && trailingNewline) sb.Append(eol);
            byte[] data = new UTF8Encoding(false).GetBytes(sb.ToString());

            // Respaldo unico: la primera vez que se va a escribir prefs se guarda
            // una copia del original de Spotify y nunca se vuelve a pisar.
            string bak = path + ".whoslistening.bak";
            if (File.Exists(path) && !File.Exists(bak))
            {
                try { File.Copy(path, bak); }
                catch (IOException ex) { Log.Error("respaldo de prefs", ex); }
                catch (UnauthorizedAccessException ex) { Log.Error("respaldo de prefs", ex); }
            }

            // Escritura atomica: se escribe un temporal y se reemplaza de una vez,
            // para que un corte no deje prefs vacio o a medias.
            string tmp = path + ".whoslistening.tmp";
            try
            {
                File.WriteAllBytes(tmp, data);
                if (File.Exists(path)) File.Replace(tmp, path, null);
                else File.Move(tmp, path);
            }
            finally
            {
                if (!File.Exists(path) && File.Exists(tmp))
                {
                    // File.Replace puede fallar despues de quitar el original
                    // (errores 1176/1177): el temporal es la unica copia, no se borra.
                    try { File.Move(tmp, path); }
                    catch (IOException ex) { Log.Error("recuperar prefs desde temporal", ex); }
                    catch (UnauthorizedAccessException ex) { Log.Error("recuperar prefs desde temporal", ex); }
                }
                else if (File.Exists(tmp))
                {
                    try { File.Delete(tmp); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        // Devuelve el indice del '=' si la linea es clave/valor, o -1.
        private static int EqIndex(string line)
        {
            int idx = line.IndexOf('=');
            return idx >= 1 ? idx : -1;
        }

        private int FindLine(string key)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsKeyLine(i, key)) return i;
            }
            return -1;
        }

        public string Get(string key)
        {
            int i = FindLine(key);
            if (i < 0) return null;
            return lines[i].Substring(EqIndex(lines[i]) + 1);
        }

        private bool IsKeyLine(int i, string key)
        {
            int idx = EqIndex(lines[i]);
            return idx >= 0 && lines[i].Substring(0, idx).Trim() == key;
        }

        // Actualiza la primera linea con esa clave y quita las repetidas.
        public void Set(string key, string rawValue)
        {
            int i = FindLine(key);
            if (i >= 0)
            {
                string old = lines[i];
                lines[i] = old.Substring(0, EqIndex(old) + 1) + rawValue;
                for (int j = lines.Count - 1; j > i; j--)
                {
                    if (IsKeyLine(j, key)) lines.RemoveAt(j);
                }
            }
            else
            {
                lines.Add(key + "=" + rawValue);
            }
        }

        // Quita todas las lineas con esa clave.
        public void Remove(string key)
        {
            for (int j = lines.Count - 1; j >= 0; j--)
            {
                if (IsKeyLine(j, key)) lines.RemoveAt(j);
            }
        }

        public Dictionary<string, string> GetAuth()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            foreach (string key in AuthKeys)
            {
                string v = Get(key);
                if (v != null) result[key] = v;
            }
            return result;
        }

        public void ApplyAuth(IDictionary<string, string> auth)
        {
            foreach (string key in AuthKeys)
            {
                string v;
                if (auth != null && auth.TryGetValue(key, out v) && !string.IsNullOrEmpty(v))
                {
                    if (!v.StartsWith("\"")) v = "\"" + v + "\"";
                    Set(key, v);
                }
                else
                {
                    Remove(key);
                }
            }
        }

        public string Username
        {
            get
            {
                string v = Get("autologin.username");
                if (v == null) return null;
                v = v.Trim();
                if (v.Length >= 2 && v.StartsWith("\"") && v.EndsWith("\""))
                    v = v.Substring(1, v.Length - 2);
                else
                    v = v.Trim('"');
                return v.Length == 0 ? null : v;
            }
        }
    }
}
