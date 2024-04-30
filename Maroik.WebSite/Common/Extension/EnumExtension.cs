//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Reflection;
//using System.ComponentModel;
//using Calyx.PathDAL.Library.Extension;

//namespace Calyx.PathDAL.Library.Extension
//{
//	public static class EnumHelper
//	{
//		public static string GetDescription(Enum en)
//		{
//			Type type = en.GetType();

//			MemberInfo[] memInfo = type.GetMember(en.ToString());

//			if (memInfo != null && memInfo.Length > 0)
//			{
//				object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

//				if (attrs != null && attrs.Length > 0)
//				{
//					return ((DescriptionAttribute)attrs[0]).Description;
//				}
//			}

//			return en.ToString();
//		}
//	}
//}

////public IEnumerable<BusinessRuleMasterInfo> GetBusinessRuleMaster(BusinessRuleName pBusinessRuleName)
////{
////    string businessRuleName = EnumHelper.GetDescription(pBusinessRuleName);
