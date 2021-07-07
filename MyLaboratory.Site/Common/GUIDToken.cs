using System;
using System.Linq;

namespace MyLaboratory.Site.Common
{
    public class GUIDToken
    {
        public static string Generate() 
        {
            // byte[] time = BitConverter.GetBytes(DateTime.UtcNow.ToBinary());
            // byte[] key = Guid.NewGuid().ToByteArray();
            //string token = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            string token = Guid.NewGuid().ToString("N").ToUpper();
            return token;
        }
    }
}