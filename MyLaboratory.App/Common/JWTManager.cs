using System;
using System.Linq;

namespace MyLaboratory.App.Common
{
    public static class JWTManager
    {
        public static string AccessToken { get; set; } // get 시, 이게 유효한지 확인하고 아니면 자동으로 로그인 페이지로 갈 것
        public static string RefreshToken { get; set; } // Refresh도 할 것
    }
}