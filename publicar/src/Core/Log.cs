using System;
using System.IO;
using System.Text;

namespace WhosListening
{
    public static class Log
    {
        private const int MaxBytes = 512 * 1024;
        private static readonly object sync = new object();

        public static void Info(string msg)
        {
            Write("INFO " + msg);
        }

        public static void Error(string msg, Exception ex)
        {
            string text = "ERROR " + msg;
            if (ex != null) text += " | " + ex.GetType().Name + ": " + ex.Message;
            Write(text);
        }

        public static string Redact(string value)
        {
            if (value == null || value.Length <= 8) return "***";
            return value.Substring(0, 8) + "…";
        }

        private static void Write(string line)
        {
            try
            {
                lock (sync)
                {
                    string file = AppPaths.LogFile;
                    Directory.CreateDirectory(Path.GetDirectoryName(file));
                    string full = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + line + "\r\n";
                    File.AppendAllText(file, full, new UTF8Encoding(false));
                    Trim(file);
                }
            }
            catch (Exception)
            {
                // El log nunca debe romper el programa.
            }
        }

        private static void Trim(string file)
        {
            FileInfo info = new FileInfo(file);
            if (info.Length <= MaxBytes) return;
            byte[] all = File.ReadAllBytes(file);
            int start = all.Length / 2;
            // Avanzar hasta después del siguiente salto de línea para no dejar una línea cortada.
            while (start < all.Length && all[start] != (byte)'\n') start++;
            start++;
            if (start >= all.Length) start = all.Length / 2;
            int count = all.Length - start;
            byte[] tail = new byte[count];
            Buffer.BlockCopy(all, start, tail, 0, count);
            File.WriteAllBytes(file, tail);
        }
    }
}
