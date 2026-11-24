using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace WhosListening
{
    public class AccountStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("WhosListening.v1");

        private class AccountFile
        {
            public string id { get; set; }
            public string label { get; set; }
            public string username { get; set; }
            public int order { get; set; }
            public string savedAtUtc { get; set; }
            public string auth { get; set; }
        }

        private readonly string accountsDir;
        private readonly List<string> unreadableFiles = new List<string>();

        public AccountStore(string accountsDir)
        {
            this.accountsDir = accountsDir;
        }

        public List<string> UnreadableFiles { get { return unreadableFiles; } }

        public List<Account> LoadAll()
        {
            unreadableFiles.Clear();
            List<Account> result = new List<Account>();
            if (!Directory.Exists(accountsDir)) return result;

            string[] files = Directory.GetFiles(accountsDir, "*.json");
            foreach (string file in files)
            {
                if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    result.Add(ReadFile(file));
                }
                catch (Exception ex)
                {
                    string name = Path.GetFileName(file);
                    unreadableFiles.Add(name);
                    Log.Error("No se pudo leer la cuenta " + name, ex);
                }
            }

            result.Sort(delegate(Account a, Account b)
            {
                int c = a.Order.CompareTo(b.Order);
                if (c != 0) return c;
                return StringComparer.CurrentCultureIgnoreCase.Compare(a.Label, b.Label);
            });
            return result;
        }

        public Account Add(string label, string username, Dictionary<string, string> auth)
        {
            string clean = ValidateLabel(label);
            List<Account> existing = LoadAll();
            int order = 0;
            bool any = false;
            foreach (Account e in existing)
            {
                if (!any || e.Order >= order) order = e.Order + 1;
                any = true;
            }

            Account a = new Account();
            a.Id = Guid.NewGuid().ToString("N");
            a.Label = clean;
            a.Username = username;
            a.Order = order;
            a.SavedAtUtc = DateTime.UtcNow;
            a.Auth = auth != null ? auth : new Dictionary<string, string>();
            Save(a);
            return a;
        }

        public void Save(Account a)
        {
            if (a == null) throw new ArgumentNullException("a");
            if (string.IsNullOrEmpty(a.Id)) throw new ArgumentException("La cuenta no tiene id.");

            AccountFile f = new AccountFile();
            f.id = a.Id;
            f.label = a.Label;
            f.username = a.Username;
            f.order = a.Order;
            f.savedAtUtc = a.SavedAtUtc.ToString("o");
            byte[] plain = Encoding.UTF8.GetBytes(Json.Serialize(a.Auth != null ? a.Auth : new Dictionary<string, string>()));
            byte[] cipher = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
            f.auth = Convert.ToBase64String(cipher);

            Directory.CreateDirectory(accountsDir);
            string target = PathFor(a.Id);
            string tmp = target + ".tmp";
            File.WriteAllText(tmp, Json.Serialize(f), new UTF8Encoding(false));
            if (File.Exists(target)) File.Replace(tmp, target, null);
            else File.Move(tmp, target);
        }

        public void Rename(string id, string newLabel)
        {
            string clean = ValidateLabel(newLabel);
            Account found = null;
            foreach (Account a in LoadAll())
            {
                if (a.Id == id) { found = a; break; }
            }
            if (found == null) throw new ArgumentException("No existe la cuenta " + id);
            found.Label = clean;
            Save(found);
        }

        public void Delete(string id)
        {
            string path = PathFor(id);
            if (File.Exists(path)) File.Delete(path);
        }

        public Account FindByUsername(string username)
        {
            if (username == null) return null;
            foreach (Account a in LoadAll())
            {
                if (string.Equals(a.Username, username, StringComparison.OrdinalIgnoreCase)) return a;
            }
            return null;
        }

        private string PathFor(string id)
        {
            Guid g;
            if (id == null || !Guid.TryParseExact(id, "N", out g))
                throw new ArgumentException("Id de cuenta no válido.");
            return Path.Combine(accountsDir, id + ".json");
        }

        private static string ValidateLabel(string label)
        {
            if (label == null || label.Trim().Length == 0)
                throw new ArgumentException("El nombre no puede estar vacío.");
            return label.Trim();
        }

        private static Account ReadFile(string file)
        {
            string text = File.ReadAllText(file, Encoding.UTF8);
            AccountFile f = Json.Deserialize<AccountFile>(text);
            if (f == null || string.IsNullOrEmpty(f.id) || f.label == null || f.username == null
                || string.IsNullOrEmpty(f.savedAtUtc) || string.IsNullOrEmpty(f.auth))
                throw new InvalidDataException("Faltan campos en el archivo de cuenta.");

            Guid g;
            if (!Guid.TryParseExact(f.id, "N", out g)
                || !string.Equals(f.id, Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Id de cuenta no válido.");

            Dictionary<string, string> auth;
            try
            {
                byte[] cipher = Convert.FromBase64String(f.auth);
                byte[] plain = ProtectedData.Unprotect(cipher, Entropy, DataProtectionScope.CurrentUser);
                auth = Json.Deserialize<Dictionary<string, string>>(Encoding.UTF8.GetString(plain));
            }
            catch (Exception)
            {
                throw new InvalidDataException("auth ilegible");
            }
            if (auth == null) throw new InvalidDataException("auth ilegible");

            Account a = new Account();
            a.Id = f.id;
            a.Label = f.label;
            a.Username = f.username;
            a.Order = f.order;
            a.SavedAtUtc = DateTime.Parse(f.savedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind);
            a.Auth = auth;
            return a;
        }
    }
}
