using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WhosListening
{
    public static class AccountStoreTests
    {
        private static Dictionary<string, string> Auth(string blob)
        {
            Dictionary<string, string> d = new Dictionary<string, string>();
            d["autologin.blob"] = blob;
            d["autologin.username"] = "user";
            return d;
        }

        public static void TestRoundTripEncrypted()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account a = store.Add("Principal", "user", Auth("SECRETO-BLOB-12345"));

            string text = File.ReadAllText(Path.Combine(dir, a.Id + ".json"), Encoding.UTF8);
            Assert.True(!text.Contains("SECRETO-BLOB-12345"), "el archivo no contiene el blob en claro");

            List<Account> all = store.LoadAll();
            Assert.Equal(1, all.Count, "una cuenta");
            Assert.Equal("SECRETO-BLOB-12345", all[0].Auth["autologin.blob"], "blob recuperado");
            Assert.Equal("Principal", all[0].Label, "label");
            Assert.Equal("user", all[0].Username, "username");
            Assert.Equal(a.Id, all[0].Id, "id");
        }

        public static void TestLabelsAreFreeTextAndIdsAreGuids()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account a = store.Add("Mamá / 🎵", "u1", Auth("b1"));
            Account b = store.Add("Mamá / 🎵", "u2", Auth("b2"));

            Assert.Equal(2, store.LoadAll().Count, "conviven dos con el mismo nombre");
            Guid parsed;
            Assert.True(Guid.TryParseExact(a.Id, "N", out parsed), "el id es un GUID");
            Assert.True(File.Exists(Path.Combine(dir, a.Id + ".json")), "el archivo se llama como el id");
            Assert.True(a.Id != b.Id, "ids distintos");
            Assert.Throws<ArgumentException>(delegate { store.Add("  ", "u3", Auth("b3")); });
        }

        public static void TestUnreadableFileIsSkippedAndReported()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account good = store.Add("Buena", "ok", Auth("bueno"));
            Account bad = store.Add("Mala", "mal", Auth("malo"));

            string badPath = Path.Combine(dir, bad.Id + ".json");
            string json = File.ReadAllText(badPath, Encoding.UTF8);
            Dictionary<string, object> obj = Json.Deserialize<Dictionary<string, object>>(json);
            byte[] junk = new byte[64];
            new Random(42).NextBytes(junk);
            obj["auth"] = Convert.ToBase64String(junk);
            File.WriteAllText(badPath, Json.Serialize(obj), new UTF8Encoding(false));

            List<Account> all = store.LoadAll();
            Assert.Equal(1, all.Count, "solo carga la buena");
            Assert.Equal(good.Id, all[0].Id, "es la buena");
            Assert.Equal(1, store.UnreadableFiles.Count, "una ilegible");
            Assert.Equal(bad.Id + ".json", store.UnreadableFiles[0], "nombre del archivo ilegible");
        }

        public static void TestDeleteAndRenameAndOrder()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account a = store.Add("Uno", "A", Auth("a"));
            Account b = store.Add("Dos", "B", Auth("b"));
            Account c = store.Add("Tres", "C", Auth("c"));
            Assert.Equal(0, a.Order, "primer orden");
            Assert.Equal(1, b.Order, "segundo orden");
            Assert.Equal(2, c.Order, "tercer orden");

            store.Rename(b.Id, "  Segunda  ");
            List<Account> all = store.LoadAll();
            Assert.Equal("Segunda", all[1].Label, "renombrada y recortada");
            Assert.Throws<ArgumentException>(delegate { store.Rename(b.Id, " "); });
            Assert.Throws<ArgumentException>(delegate { store.Rename("noexiste", "X"); });

            Assert.Equal(b.Id, store.FindByUsername("b").Id, "busca sin distinguir mayúsculas");
            Assert.True(store.FindByUsername("zzz") == null, "no encontrada es null");

            store.Delete(a.Id);
            store.Delete(a.Id);
            all = store.LoadAll();
            Assert.Equal(2, all.Count, "quedan dos");
            Assert.Equal(b.Id, all[0].Id, "orden conservado");
            Assert.Equal(c.Id, all[1].Id, "orden conservado 2");

            Account d = store.Add("Cuatro", "D", Auth("d"));
            Assert.Equal(3, d.Order, "orden = máximo + 1");
        }

        public static void TestHostileIdIsRejected()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account good = store.Add("Buena", "ok", Auth("bueno"));
            Account other = store.Add("Otra", "otra", Auth("otro"));

            string otherPath = Path.Combine(dir, other.Id + ".json");
            Dictionary<string, object> obj = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(otherPath, Encoding.UTF8));
            obj["id"] = "..\\..\\evil";
            File.WriteAllText(otherPath, Json.Serialize(obj), new UTF8Encoding(false));

            List<Account> all = store.LoadAll();
            Assert.Equal(1, all.Count, "solo carga la buena");
            Assert.Equal(good.Id, all[0].Id, "es la buena");
            Assert.Equal(1, store.UnreadableFiles.Count, "id hostil ilegible");
            Assert.Equal(other.Id + ".json", store.UnreadableFiles[0], "nombre del archivo");

            Account hostile = new Account();
            hostile.Id = "..\\x";
            hostile.Label = "X";
            hostile.Username = "x";
            Assert.Throws<ArgumentException>(delegate { store.Save(hostile); });
            string parent = Path.GetDirectoryName(dir);
            Assert.True(!File.Exists(Path.Combine(parent, "x.json")), "no se escribió fuera de accounts");
        }

        public static void TestCorruptAuthReportsGenericError()
        {
            string dir = AppPaths.AccountsDir;
            AccountStore store = new AccountStore(dir);
            Account a = store.Add("Rota", "rota", Auth("secreto"));

            string path = Path.Combine(dir, a.Id + ".json");
            Dictionary<string, object> obj = Json.Deserialize<Dictionary<string, object>>(File.ReadAllText(path, Encoding.UTF8));
            byte[] junk = new byte[64];
            new Random(7).NextBytes(junk);
            obj["auth"] = Convert.ToBase64String(junk);
            File.WriteAllText(path, Json.Serialize(obj), new UTF8Encoding(false));

            Assert.Equal(0, store.LoadAll().Count, "se omite la cuenta rota");
            Assert.Equal(1, store.UnreadableFiles.Count, "queda reportada");
            string log = File.ReadAllText(AppPaths.LogFile, Encoding.UTF8);
            Assert.True(log.Contains("auth ilegible"), "el log trae el mensaje genérico");
            Assert.True(!log.Contains("CryptographicException"), "el log no trae el tipo interno");
        }
    }
}
