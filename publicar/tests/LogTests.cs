using System.IO;

namespace WhosListening
{
    public static class LogTests
    {
        public static void TestRedact()
        {
            Assert.Equal("abcdefgh…", Log.Redact("abcdefghijk"), "");
            Assert.Equal("***", Log.Redact("abc"), "");
        }

        public static void TestTrimsAt512KB()
        {
            string filler = new string('x', 90);
            for (int i = 0; i < 6000; i++)
            {
                Log.Info("linea " + i + " " + filler);
            }
            Log.Info("ULTIMA-LINEA-ESCRITA");

            FileInfo info = new FileInfo(AppPaths.LogFile);
            Assert.True(info.Length <= 512 * 1024, "el archivo mide " + info.Length + " bytes");
            string text = File.ReadAllText(AppPaths.LogFile);
            Assert.True(text.Contains("ULTIMA-LINEA-ESCRITA"), "falta la ultima linea");
        }
    }
}
