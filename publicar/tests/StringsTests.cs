using System.Collections.Generic;

namespace WhosListening
{
    public static class StringsTests
    {
        public static void TestEveryLanguageHasEveryKey()
        {
            List<string> en = new List<string>(Strings.KeysFor("en"));
            Assert.True(en.Count > 0, "en no tiene claves");
            foreach (string lang in Strings.Languages)
            {
                List<string> keys = new List<string>(Strings.KeysFor(lang));
                foreach (string k in en) Assert.True(keys.Contains(k), lang + " no tiene la clave " + k);
                foreach (string k in keys) Assert.True(en.Contains(k), lang + " tiene clave extra " + k);
            }
        }

        public static void TestFallbackToEnglish()
        {
            Strings.SetLanguage("ja");
            Assert.Equal("en", Strings.Current, "");
            Strings.SetLanguage(null);
            Assert.Equal("en", Strings.Current, "");
        }

        public static void TestSpanishTitle()
        {
            Strings.SetLanguage("es");
            Assert.Equal("¿Quién está escuchando?", Strings.Get("title"), "");
            Assert.Equal("En uso", Strings.Get("inUse"), "");
            Assert.Equal("Mostrar al abrir Spotify", Strings.Get("toggle"), "");
            Assert.Equal("Diagnóstico", Strings.Get("diagnostics"), "");
            Strings.SetLanguage("en");
            Assert.Equal("Who's listening?", Strings.Get("title"), "");
        }

        public static void TestFormatPlaceholders()
        {
            foreach (string key in Strings.KeysFor("en"))
            {
                Strings.SetLanguage("en");
                if (!Strings.Get(key).Contains("{0}")) continue;
                foreach (string lang in Strings.Languages)
                {
                    Strings.SetLanguage(lang);
                    Assert.True(Strings.Get(key).Contains("{0}"), lang + " pierde {0} en " + key);
                }
            }
            Strings.SetLanguage("es");
            Assert.Equal("Cuenta guardada: Ana", Strings.Format("statusSaved", "Ana"), "");
        }

        public static void TestSelectorKeysSpanish()
        {
            Strings.SetLanguage("es");
            Assert.Equal("Elige una cuenta para abrir Spotify", Strings.Get("subtitle"), "");
            Assert.Equal("Perfil", Strings.Get("profile"), "");
            Assert.Equal("Nueva o actual", Strings.Get("addAccountSub"), "");
            Assert.Equal("No hay una cuenta con sesión iniciada en Spotify.", Strings.Get("errNoCurrentAccount"), "");
        }

        public static void TestImportStringsAreCountNeutral()
        {
            Strings.SetLanguage("es");
            Assert.True(Strings.Get("importPrompt").Contains("{0}"), "importPrompt es tiene {0}");
            Assert.Equal("Cuentas guardadas por un selector de cuentas anterior: 1. ¿Importarlas?", Strings.Format("importPrompt", 1), "");
            Assert.Equal("Cuentas importadas: 3.", Strings.Format("importResult", 3), "");
            Assert.Equal("Who's Listening? quedó instalado. Ábrelo desde el menú Inicio cuando quieras cambiar de cuenta.", Strings.Get("installedNoStartup"), "");
            Strings.SetLanguage("fr");
            Assert.True(!Strings.Get("uninstallConfirm").Contains("??"), "fr uninstallConfirm sin ??");
            Assert.Equal("Actif", Strings.Get("inUse"), "");
            Strings.SetLanguage("de");
            Assert.Equal("Aktiv", Strings.Get("inUse"), "");
            Strings.SetLanguage("es");
        }

        public static void TestMissingKeyReturnsKey()
        {
            Strings.SetLanguage("es");
            Assert.Equal("noExiste", Strings.Get("noExiste"), "");
        }
    }
}
