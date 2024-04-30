//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;


//    public static class ConvertStringExtension
//    {
//        public static string ConvertToNumber(this string s)
//        {
//            int value;

//            int.TryParse(s, out value);

//            return value.ToString();
//        }

//        public static string ConvertToBooleanString(this string s)
//        {
//            if (s.Length != 1)
//                return s;

//            if (s.Equals("Y"))
//                return "True";

//            if (s.Equals("N"))
//                return "False";

//            return s;
//        }

//        public static string GetNumber(this string s)
//        {
//            return new String((from t in s
//                              where char.IsDigit(t)
//                              select t).ToArray());
//        }
//    }

