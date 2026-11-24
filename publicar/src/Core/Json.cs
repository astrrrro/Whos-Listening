using System.Web.Script.Serialization;

namespace WhosListening
{
    public static class Json
    {
        public static string Serialize(object o)
        {
            JavaScriptSerializer s = new JavaScriptSerializer();
            return s.Serialize(o);
        }

        public static T Deserialize<T>(string s)
        {
            JavaScriptSerializer ser = new JavaScriptSerializer();
            return ser.Deserialize<T>(s);
        }
    }
}
