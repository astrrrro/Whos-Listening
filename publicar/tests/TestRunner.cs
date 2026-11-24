using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace WhosListening
{
    public static class TestRunner
    {
        public static int Main(string[] args)
        {
            List<string> lines = new List<string>();
            int failures = 0;
            string tempBase = Path.Combine(Path.GetTempPath(), "WhosListening-tests");

            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            Array.Sort(types, delegate(Type a, Type b) { return string.CompareOrdinal(a.FullName, b.FullName); });

            foreach (Type type in types)
            {
                if (!type.IsClass || !type.Name.EndsWith("Tests", StringComparison.Ordinal)) continue;
                MethodInfo[] methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
                Array.Sort(methods, delegate(MethodInfo a, MethodInfo b) { return string.CompareOrdinal(a.Name, b.Name); });
                foreach (MethodInfo method in methods)
                {
                    if (!method.Name.StartsWith("Test", StringComparison.Ordinal)) continue;
                    if (method.ReturnType != typeof(void) || method.GetParameters().Length != 0) continue;

                    string name = type.Name + "." + method.Name;
                    string root = Path.Combine(tempBase, Guid.NewGuid().ToString("N"));
                    string appData = Path.Combine(root, "appdata");
                    string localAppData = Path.Combine(root, "localappdata");
                    string result;
                    try
                    {
                        Directory.CreateDirectory(appData);
                        Directory.CreateDirectory(localAppData);
                        AppPaths.OverrideRoots(appData, localAppData);
                        method.Invoke(null, null);
                        result = "OK " + name;
                    }
                    catch (Exception ex)
                    {
                        Exception inner = ex;
                        TargetInvocationException tie = ex as TargetInvocationException;
                        if (tie != null && tie.InnerException != null) inner = tie.InnerException;
                        failures++;
                        result = "FALLA " + name + ": " + OneLine(inner.GetType().Name + " " + inner.Message);
                    }
                    finally
                    {
                        AppPaths.OverrideRoots(null, null);
                        try { Directory.Delete(root, true); } catch (Exception) { }
                    }
                    lines.Add(result);
                    Console.WriteLine(result);
                }
            }

            string final = failures == 0 ? "RESULTADO: OK" : "RESULTADO: " + failures + " FALLAS";
            lines.Add(final);
            Console.WriteLine(final);

            string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tests.log");
            File.WriteAllText(logPath, string.Join("\r\n", lines.ToArray()) + "\r\n", new UTF8Encoding(false));
            return failures;
        }

        private static string OneLine(string s)
        {
            return s.Replace("\r", " ").Replace("\n", " ");
        }
    }
}
