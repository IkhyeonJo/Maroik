namespace Maroik.WebSite.Helpers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class RequiredHttpPostAccessAttribute : Attribute
    {
        public string Role { get; set; }
    }
}
