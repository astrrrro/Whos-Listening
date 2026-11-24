using System;

namespace WhosListening
{
    public class AssertException : Exception
    {
        public AssertException(string message) : base(message) { }
    }

    public static class Assert
    {
        public static void Equal(object expected, object actual, string msg)
        {
            if (!object.Equals(expected, actual))
            {
                throw new AssertException(Prefix(msg) + "esperado <" + Show(expected) + "> pero fue <" + Show(actual) + ">");
            }
        }

        public static void True(bool cond, string msg)
        {
            if (!cond) throw new AssertException(Prefix(msg) + "la condicion era falsa");
        }

        public static void Throws<T>(Action a) where T : Exception
        {
            try
            {
                a();
            }
            catch (T)
            {
                return;
            }
            catch (Exception ex)
            {
                throw new AssertException("se esperaba " + typeof(T).Name + " pero se lanzo " + ex.GetType().Name);
            }
            throw new AssertException("se esperaba " + typeof(T).Name + " pero no se lanzo nada");
        }

        private static string Prefix(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return "";
            return msg + ": ";
        }

        private static string Show(object o)
        {
            return o == null ? "null" : o.ToString();
        }
    }
}
