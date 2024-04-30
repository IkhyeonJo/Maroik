//using System;
//using System.Web;
//using Calyx.PathDAL.Security;
//using System.Collections.Generic;

//public static class DataExtension
//{
//    #region New DateTime

//    public static DateTime? ToLocalDateTimeByLoSOUTimeZone(this DateTime? dt)
//    {
//        if (dt.HasValue)
//        {
//            return ToLocalDateTimeByLoSOUTimeZone(dt.Value);
//        }
//        return dt;
//    }

//    public static DateTime? ToLocalDateTimeByLoSOUTimeZone(this DateTime dt)
//    {
//        if (HttpContext.Current != null)
//        {
//            var user = HttpContext.Current.User as PathPrincipal;
//            if (user != null)
//            {
//                return TimeZoneInfo.ConvertTimeFromUtc(dt, user.Identity.LoSOUTimeZone);
//            }
//        }

//        return dt;
//    }

//    public static DateTime? ToLocalDateTimeByTimezone(this DateTime? dt)
//	{
//		if (dt.HasValue)
//		{
//			return ToLocalDateTimeByTimezone(dt.Value);
//		}

//		return dt;
//	}

//    public static string ToLocalDateTimeByTimezoneMMddYYYY(this DateTime? dt)
//    {
//        DateTime? result = null;
//        if (dt.HasValue)
//        {
//            result = ToLocalDateTimeByTimezone(dt.Value);
//        }
//        return (result == null) ? "" : ((DateTime)result).ToString("MM/dd/yyyy");
//    }

//    public static DateTime? ToLocalDateTimeByTimezone(this DateTime dt)
//	{
//		if (HttpContext.Current != null)
//		{
//			var user = HttpContext.Current.User as PathPrincipal;
//			if (user != null)
//			{
//				return TimeZoneInfo.ConvertTimeFromUtc(dt, user.Identity.LocalTimezone);
//			}
//		}

//		return dt;
//	}

//	#endregion

//	#region DateTime Type

//	private static readonly long DatetimeMinTimeTicks = (new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Ticks;


//	public static long ToJavaScriptMilliseconds(this DateTime dt)
//	{
//		return (long)((dt.Ticks - DatetimeMinTimeTicks) / 10000);
//	}

//	public static long ToJavaScriptMilliseconds(this DateTime? dt)
//	{
//		if (dt.HasValue)
//		{
//			return ToJavaScriptMilliseconds(dt.Value);
//		}

//		return 0;
//	}

//    /// <summary>
//    /// Return a localized and formatted date value.
//    /// The format is MM/dd/yyyy
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="utcDateTime"></param>
//    /// <returns></returns>
//    public static string ToLocalDateString(this DateTime? utcDateTime)
//    {
//        if (null == utcDateTime)
//            return string.Empty;

//        //int hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset;
//        //DateTime localDateTime = ((DateTime)utcDateTime).AddHours((-hourOffset)); // Wayne removed 12/15/2014. Because of summar time zone problem.

//        DateTime localDateTime = ((DateTime)utcDateTime); // Wayne added 12/15/2014. Because of summar time zone problem.
//        return localDateTime.ToString("MM/dd/yyyy");
//    }

//    /// <summary>
//    /// Return a localized and formatted date value.
//    /// The format is MM/dd/yyyy
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="utcDateTime"></param>
//    /// <returns></returns>
//    public static string ToLocalDateString(this DateTime utcDateTime)
//    {
//        return ToLocalDateString((DateTime?)utcDateTime);
//    }

//    public static string ToLocalDateString(this DateTime utcDateTime, int hourOffset)
//    {
//        DateTime localDateTime = ((DateTime)utcDateTime).AddHours(-hourOffset);
//        return localDateTime.ToString("MM/dd/yyyy");
//    }

//    public static string ToLocalDateStringByLogan(this DateTime? utcDateTime)
//    {
//        if (null == utcDateTime)
//            return null;

//        int hourOffset;

//        try
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset;
//        }
//        catch (Exception)
//        {
//            hourOffset = 5;
//        }

//        DateTime localDateTime = ((DateTime)utcDateTime).AddHours(-hourOffset);
//        return localDateTime.ToString("MM/dd/yyyy");
//    }

//    /// <summary>
//    /// Return a localized nullable datetime value.
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="utcDateTime"></param>
//    /// <returns></returns>
//    public static DateTime? ToLocalDateTime(this DateTime? utcDateTime)
//    {
//        if (null == utcDateTime)
//            return null;

//        int hourOffset;

//        try
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset;
//        }
//        catch (Exception)
//        {
//            hourOffset = 5;
//        }


//        return ((DateTime)utcDateTime).AddHours(-hourOffset);
//    }

//    public static DateTime? ToLocalDateTime(this DateTime? utcDateTime, int hourOffset)
//    {
//        if (null == utcDateTime)
//            return null;

//        if (HttpContext.Current != null)
//        {
//            try
//            {
//                return ((DateTime)utcDateTime).AddHours(-((PathPrincipal)HttpContext.Current.User).Identity.HourOffset);
//            }
//            catch (Exception e)
//            {
//                return ((DateTime)utcDateTime).AddHours(-hourOffset);
//            }
//        }
//        else
//        {
//            return ((DateTime)utcDateTime).AddHours(-hourOffset);   
//        }
//    }

//    /// <summary>
//    /// Return a localized nullable datetime value.
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="utcDateTime"></param>
//    /// <returns></returns>
//    public static DateTime? ToLocalDateTime(this DateTime utcDateTime)
//    {
//        return ToLocalDateTime((DateTime?)utcDateTime);
//    }

//    public static DateTime? ToLocalDateTime(this DateTime utcDateTime, int hourOffset)
//    {
//        return ToLocalDateTime((DateTime?)utcDateTime, hourOffset);
//    }

//	public static string ToShortTimeString(this DateTime? datetime)
//	{
//		if(datetime.HasValue)
//			return datetime.Value.ToShortTimeString();
//		else
//			return String.Empty;
//	}

//    /// <summary>
//    /// Return a UTC DateTIme value.
//    /// </summary>
//    /// <param name="localizedDateTime">localized DateTime value</param>
//    /// <returns>UTC DateTime value</returns>
//    public static DateTime? ToUTCDateTime(this DateTime? localizedDateTime)
//    {
//        if (null == localizedDateTime)
//        {
//            return null;
//        }
//        int hourOffset;
//        try
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset;            
//        } catch(Exception)
//        {
//            hourOffset = 5;                
//        }


//        return ((DateTime)localizedDateTime).AddHours(hourOffset);
//    }

//    /// <summary>
//    /// Return a UTC DateTIme value.
//    /// </summary>
//    /// <param name="localizedDateTime">localized DateTime value</param>
//    /// <returns>UTC DateTime value</returns>
//    public static DateTime? ToUTCDateTime(this DateTime localizedDateTime)
//    {
//        return ToUTCDateTime((DateTime?)localizedDateTime);
//    }

//    public static DateTime? ToUTCDateTime(this DateTime localizedDateTime, int hourOffset)
//    {
//        return ToUTCDateTime((DateTime?)localizedDateTime, hourOffset);
//    }

//    public static DateTime? ToUTCDateTime(this DateTime? localizedDateTime, int hourOffset)
//    {
//        if (null == localizedDateTime)
//        {
//            return null;
//        }

//        return ((DateTime)localizedDateTime).AddHours(hourOffset);
//    }

//    public static string ToStartMonth(this DateTime date)
//    {
//        int year = date.Year;
//        return year + "-01";
//    }

//    public static string ToEndMonth(this DateTime date)
//    {
//        int year = date.Year;
//        return year + "-12";
//    }

//    public static string ToNowMonth(this DateTime date)
//    {
//        string year = date.ToString("yyyy");
//        string month = date.ToString("MM");
//        return year + "-" + month;
//    }

//    public static DateTime? SetUTCStartDate(this DateTime? startDate)
//    {
//        if (null == startDate)
//            return null;

//        DateTime tempDate = (DateTime)startDate;

//        tempDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, 0, 0, 0);
//        return tempDate.ToUTCDateTime();
//    }

//    public static DateTime? SetUTCStartDate(this DateTime startDate)
//    {
//        return SetUTCStartDate((DateTime?)startDate);
//    }

//    public static DateTime? SetUTCStartDate(this DateTime? startDate, int hourOffset)
//    {
//        if (null == startDate)
//            return null;

//        DateTime tempDate = (DateTime)startDate;

//        tempDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, 0, 0, 0);
//        return tempDate.ToUTCDateTime(hourOffset);
//    }

//    public static DateTime? SetUTCEndDate(this DateTime? endDate)
//    {
//        if (null == endDate)
//            return null;

//        DateTime tempDate = (DateTime)endDate;

//        tempDate = new DateTime(tempDate.Year, tempDate.Month, tempDate.Day, 23, 59, 59);
//        return tempDate.ToUTCDateTime();
//    }

//    public static DateTime? SetUTCEndDate(this DateTime endDate)
//    {
//        return SetUTCEndDate((DateTime?)endDate);
//    }

//    /// <summary>
//    /// Convert a nullable datetime value to a datetime value.
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="dateTime"></param>
//    /// <returns></returns>
//    public static DateTime ToDateTime(this DateTime? dateTime)
//    {
//        if (null == dateTime)
//            return DateTime.MinValue;

//        return (DateTime)dateTime;
//    }

//	public static string ToShortDateString(this DateTime? dateTime)
//	{
//		if(dateTime.HasValue)
//			return dateTime.Value.ToShortDateString();
//		else
//			return String.Empty;
//	}


//    /// <summary>
//    /// Convert a datetime value to a nullable datetime value.
//    /// 12/02/2011 Beren Added
//    /// </summary>
//    /// <param name="dateTime"></param>
//    /// <returns></returns>
//    public static DateTime? ToNullableDateTime(this DateTime dateTime)
//    {
//        return dateTime;
//    }

//	public static int WeekNumber(this DateTime? dateTime)
//	{
//		if (dateTime.HasValue)
//			return WeekNumber(dateTime.Value);
//		else
//			return -1;

//	}

//	public static int WeekNumber(this DateTime dateTime)
//	{
//		return System.Globalization.CultureInfo.CurrentCulture.Calendar
//			.GetWeekOfYear(dateTime, System.Globalization.CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);

//	}


//    #endregion

//    #region for decimal

//    public static string ToCurrencyString(this decimal? val)
//    {
//        return SetPrimeNumberLength(val, 2);
//    }

//    public static string ToCurrencyString(this decimal val)
//    {
//        return ToCurrencyString((decimal?)val);
//    }

//    public static string ToPercentString(this decimal? val)
//    {
//        return SetPrimeNumberLength(val, 3);
//    }

//    public static string ToPercentString(this decimal val)
//    {
//        return ToPercentString((decimal?)val);
//    }

//    private static string SetPrimeNumberLength(this decimal? val, int length)
//    {
//        if (null == val/* || (decimal)0 == (decimal)val*/)
//            return string.Empty;

//        return ((decimal)val).ToString("n" + length.ToString());
//    }

//	public static string ToDecimalString(this decimal? val)
//	{
//		if (val.HasValue)
//		{
//			return ToDecimalString(val.Value);
//		}
//		return string.Empty;
//	}

//	public static string ToDecimalString(this decimal val)
//	{
//		return val.ToString();
//	}

//    #endregion

//	#region for boolean
	
//	public static string ToYesNoString(this bool? val)
//	{
//		if(val.HasValue)
//			return ToYesNoString(val.Value);
//		else
//			return String.Empty;
//	}

//	public static string ToYesNoString(this bool val)
//	{
//		if(val)
//			return "Yes";
//		else
//			return "No";
//	}

//	public static string ToBooleanString(this bool? pValue)
//	{
//		if (pValue.HasValue) return ToBooleanString(pValue.Value);

//		return string.Empty;
//	}

//	public static string ToBooleanString(this bool pValue)
//	{
//		return pValue.ToString();
//	}

//	#endregion

//    #region for string

//    public static string GetView(this string val)
//    {
//        return val ?? string.Empty;
//    }

//	public static string Subb(this string val, int length)
//	{
//		if(String.IsNullOrEmpty(val) || val.Length <= length)
//			return val;
//		else
//			return val.Substring(0, length);
//	}

//    #endregion

//	#region for double

//	public static string SetPrimeNumberLength(this double val, int length)
//	{
//		return val.ToString("n" + length);
//	}

//	public static string SetPrimeNumberLength(this double? val, int length)
//	{
//		if (val.HasValue)
//		{
//			return val.Value.ToString("n" + length);
//		}
//		return string.Empty;
//	}

//	#endregion

//	#region Calyx

//	public static string ToCalyxDallor(this decimal val)
//	{
//		return "$ " + val.ToString("n2");
//	}

//    public static string ToCalyxDallor(this decimal? val)
//    {
//        if (val.HasValue)
//        {
//			return ToCalyxDallor(val.Value);
//        }
//        return string.Empty;
//    }

//	public static string ToCalyxPercent(this decimal val)
//	{
//		return val.ToString("n3") + "%";
//	}

//	public static string ToCalyxPercent(this decimal? val)
//	{
//		if (val.HasValue)
//		{
//			return ToCalyxPercent(val.Value);
//		}
//		return string.Empty;
//	}

//	public static string ToCalyx4Scale(this decimal val)
//	{
//		return val.ToString("n4");
//	}

//	public static string ToCalyx4Scale(this decimal? val)
//	{
//		if (val.HasValue)
//		{
//			return ToCalyx4Scale(val.Value);
//		}
//		return string.Empty;
//	}


//    public static string ToCalyxSSN(this string val)
//    {
//        if (val == null || val.Length < 9) return string.Empty;
//        return val.Substring(0, 3) + "-" + val.Substring(3, 2) + "-" + val.Substring(5, 4);
//    }

//    public static string ToCalyxDate(this DateTime? val)
//    {
//        if (val.HasValue) return ToCalyxDate(val.Value);
//        return string.Empty;
//    }

//    public static string ToCalyxUtcDate(this DateTime? val)
//    {
//        if (val.HasValue) return ToCalyxDate(val.Value);
//        return string.Empty;
//    }


//    public static string ToCalyxDate(this DateTime val)
//    {
//        int hourOffset = -5;
//        var _User = HttpContext.Current.User as PathPrincipal;
//        if (_User != null)
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset * -1;
            
//        }
//        val = val.AddHours(hourOffset);
//        return String.Format("{0:MM/dd/yyyy}", val).Replace("-", "/");
//    }


//    public static string ToCalyxFullDate(this DateTime val)
//    {
//        int hourOffset = -5;
//        var _User = HttpContext.Current.User as PathPrincipal;
//        if (_User != null)
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset * -1;

//        }
//        val = val.AddHours(hourOffset);
//        return String.Format("{0:MM/dd/yyyy hh:mm:ss tt}", val).Replace("-", "/");
//    }

//    public static string ToCalyxFullDate(this DateTime? val)
//    {
//        if (val.HasValue) return ToCalyxFullDate(val.Value);
//        return string.Empty;
//    }

//	public static DateTime ToLocalDate(this DateTime utcDateTime)
//	{
//		int hourOffset;
//        try
//        {
//            hourOffset = ((PathPrincipal)HttpContext.Current.User).Identity.HourOffset;            
//        } catch(Exception)
//        {
//            hourOffset = 5;                
//        }

//		DateTime result = utcDateTime.AddHours(-hourOffset);
//		result = new DateTime(result.Year, result.Month, result.Day);
//		return result;
//	}
//	public static DateTime? ToLocalDate(this DateTime? utcDateTime)
//	{
//		if (null == utcDateTime)
//			return null;

//		return ToLocalDate(utcDateTime.Value);
//	}

//	public static string ToDateString(this DateTime val)
//	{
//		return val.ToString("MM/dd/yyyy");
//	}

//	public static string ToDateString(this DateTime? val)
//	{
//		if (val.HasValue)
//		{
//			return ToDateString(val.Value);
//		}
//		return string.Empty;
//	}


//    #endregion

//    #region for int?
//    public static bool IsNullOrZero(this int? val)
//    {
//        return (val == null || val == 0) ? true : false;
//    }
//    #endregion

//    #region for long?
//    public static bool IsNullOrZero(this long? val)
//    {
//        return (val == null || val == 0) ? true : false;
//    }
//    #endregion
    
//    #region for IEnumerable<T>
//    public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
//	{
//		HashSet<TKey> seenKeys = new HashSet<TKey>();
//		foreach (TSource element in source)
//		{
//			if (seenKeys.Add(keySelector(element)))
//			{
//				yield return element;
//			}
//		}
//	}
//	#endregion
//}
