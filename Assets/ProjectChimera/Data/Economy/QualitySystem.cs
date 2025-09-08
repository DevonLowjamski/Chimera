using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Data.Economy
{
    /// <summary>
    /// Quality grade enum for product evaluation
    /// </summary>
    public enum QualityGrade
    {
        Poor = -1,
        BelowStandard = 0,
        Acceptable = 1,
        Standard = 2,
        Good = 3,
        Excellent = 4,
        Premium = 5
    }

    /// <summary>
    /// Extension class for QualityGrade operations
    /// </summary>
    public static class QualityGradeExtensions
    {
        /// <summary>
        /// Convert QualityGrade to float value (-0.2 to 1.0)
        /// </summary>
        public static float ToFloat(this QualityGrade grade)
        {
            switch (grade)
            {
                case QualityGrade.Poor:         return -0.2f;
                case QualityGrade.BelowStandard: return 0.1f;
                case QualityGrade.Acceptable:   return 0.3f;
                case QualityGrade.Standard:     return 0.5f;
                case QualityGrade.Good:         return 0.7f;
                case QualityGrade.Excellent:    return 0.85f;
                case QualityGrade.Premium:      return 0.95f;
                default:                        return 0.5f;
            }
        }

        /// <summary>
        /// Convert QualityGrade to int value
        /// </summary>
        public static int ToInt(this QualityGrade grade)
        {
            return (int)grade;
        }

        /// <summary>
        /// Convert float value to QualityGrade
        /// </summary>
        public static QualityGrade FromFloat(float value)
        {
            if (value < 0.0f) return QualityGrade.Poor;
            if (value <= 0.2f) return QualityGrade.BelowStandard;
            if (value <= 0.4f) return QualityGrade.Acceptable;
            if (value <= 0.6f) return QualityGrade.Standard;
            if (value <= 0.8f) return QualityGrade.Good;
            if (value <= 0.9f) return QualityGrade.Excellent;
            return QualityGrade.Premium;
        }

        /// <summary>
        /// Convert int value to QualityGrade
        /// </summary>
        public static QualityGrade FromInt(int value)
        {
            return (QualityGrade)Mathf.Clamp(value, -1, 5);
        }

        /// <summary>
        /// Compare QualityGrade with float
        /// </summary>
        public static bool IsGreaterThan(this QualityGrade grade, float value)
        {
            return grade.ToFloat() > value;
        }

        /// <summary>
        /// Compare QualityGrade with float
        /// </summary>
        public static bool IsLessThan(this QualityGrade grade, float value)
        {
            return grade.ToFloat() < value;
        }

        /// <summary>
        /// Safe comparison with float using tolerance
        /// </summary>
        public static bool Equals(this QualityGrade grade, float value, float tolerance = 0.01f)
        {
            return Mathf.Abs(grade.ToFloat() - value) < tolerance;
        }

        /// <summary>
        /// Convert float to int with rounding
        /// </summary>
        public static int FloatToInt(float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion for quantity calculations
        /// </summary>
        public static int ToQuantity(float value)
        {
            return Mathf.Max(0, Mathf.RoundToInt(value));
        }

        /// <summary>
        /// Convert QualityGrade to long for database operations
        /// </summary>
        public static long ToLong(this QualityGrade grade)
        {
            return (long)(int)grade;
        }

        /// <summary>
        /// Convert long to QualityGrade
        /// </summary>
        public static QualityGrade FromLong(long value)
        {
            return (QualityGrade)Mathf.Clamp((int)value, 0, 5);
        }

        /// <summary>
        /// Comparison methods - use these instead of < > operators since enums already have natural comparison
        /// </summary>
        public static bool IsLowerThan(this QualityGrade left, QualityGrade right)
        {
            return (int)left < (int)right;
        }

        public static bool IsHigherThan(this QualityGrade left, QualityGrade right)
        {
            return (int)left > (int)right;
        }

        public static bool IsLowerOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left <= (int)right;
        }

        public static bool IsHigherOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left >= (int)right;
        }

        /// <summary>
        /// Alias for IsHigherOrEqual for consistency
        /// </summary>
        public static bool IsGreaterThanOrEqual(this QualityGrade left, QualityGrade right)
        {
            return (int)left >= (int)right;
        }

        /// <summary>
        /// Helper methods for common quality operations
        /// </summary>
        public static QualityGrade Min(QualityGrade a, QualityGrade b)
        {
            return (int)a <= (int)b ? a : b;
        }

        public static QualityGrade Max(QualityGrade a, QualityGrade b)
        {
            return (int)a >= (int)b ? a : b;
        }

        /// <summary>
        /// Calculate average quality grade from a collection
        /// </summary>
        public static QualityGrade Average(IEnumerable<QualityGrade> grades)
        {
            if (!grades.Any()) return QualityGrade.Standard;
            
            float average = grades.Select(g => g.ToFloat()).Average();
            return FromFloat(average);
        }

        /// <summary>
        /// Additional conversion helper methods
        /// </summary>
        public static int ConvertToInt(this QualityGrade grade)
        {
            return (int)grade;
        }

        public static float ConvertToFloat(this QualityGrade grade)
        {
            return grade.ToFloat();
        }

        public static long ConvertToLong(this QualityGrade grade)
        {
            return grade.ToLong();
        }

        /// <summary>
        /// Safe conversion for nullable DateTime
        /// </summary>
        public static DateTime? ToNullableDateTime(DateTime? dateTime)
        {
            return dateTime;
        }

        public static DateTime GetValueOrNow(DateTime? dateTime)
        {
            return dateTime ?? DateTime.Now;
        }

        /// <summary>
        /// Extension method to check if DateTime has a meaningful value
        /// </summary>
        /// <param name="dateTime">DateTime to check</param>
        /// <returns>True if datetime is not MinValue or default</returns>
        public static bool HasValue(this DateTime dateTime)
        {
            return dateTime != DateTime.MinValue && dateTime != default(DateTime);
        }

        /// <summary>
        /// Convert List of QualityGrade to List of float
        /// </summary>
        public static List<float> ToFloatList(this List<QualityGrade> grades)
        {
            var result = new List<float>();
            foreach (var grade in grades)
            {
                result.Add(grade.ToFloat());
            }
            return result;
        }

        /// <summary>
        /// Convert List of float to List of QualityGrade
        /// </summary>
        public static List<QualityGrade> ToQualityGradeList(this List<float> values)
        {
            var result = new List<QualityGrade>();
            foreach (var value in values)
            {
                result.Add(FromFloat(value));
            }
            return result;
        }

        /// <summary>
        /// Calculate standard deviation of quality grades
        /// </summary>
        public static float CalculateStandardDeviation(this List<QualityGrade> grades)
        {
            if (grades == null || grades.Count <= 1) return 0f;
            
            var floatValues = grades.ToFloatList();
            float average = floatValues.Sum() / floatValues.Count;
            float sumOfSquares = floatValues.Sum(x => (x - average) * (x - average));
            return UnityEngine.Mathf.Sqrt(sumOfSquares / (floatValues.Count - 1));
        }

        /// <summary>
        /// Convert List of QualityDataPoint to List of QualityGrade
        /// </summary>
        public static List<QualityGrade> ToQualityGradeList(this List<QualityDataPoint> dataPoints)
        {
            var result = new List<QualityGrade>();
            if (dataPoints != null)
            {
                foreach (var point in dataPoints)
                {
                    result.Add(point.Quality);
                }
            }
            return result;
        }

        /// <summary>
        /// Convert List of QualityDataPoint to List of float values
        /// </summary>
        public static List<float> ToFloatValueList(this List<QualityDataPoint> dataPoints)
        {
            var result = new List<float>();
            if (dataPoints != null)
            {
                foreach (var point in dataPoints)
                {
                    result.Add(point.QualityValue);
                }
            }
            return result;
        }

        /// <summary>
        /// Safe DateTime conversion from nullable
        /// </summary>
        public static DateTime ToDateTime(this DateTime? nullableDateTime)
        {
            return nullableDateTime ?? DateTime.Now;
        }

        /// <summary>
        /// Safe DateTime conversion with default value
        /// </summary>
        public static DateTime ToDateTimeOrDefault(this DateTime? nullableDateTime, DateTime defaultValue)
        {
            return nullableDateTime ?? defaultValue;
        }

        /// <summary>
        /// Safe List<string> count conversion to int
        /// </summary>
        public static int ToCount(this List<string> list)
        {
            return list?.Count ?? 0;
        }

        /// <summary>
        /// Safe List<T> count conversion to int
        /// </summary>
        public static int ToCount<T>(this List<T> list)
        {
            return list?.Count ?? 0;
        }

        /// <summary>
        /// Explicit conversion from QualityGrade to float (for method parameters)
        /// </summary>
        public static float AsFloat(this QualityGrade grade)
        {
            return grade.ToFloat();
        }

        /// <summary>
        /// Explicit conversion from float to QualityGrade (for assignments)
        /// </summary>
        public static QualityGrade AsQualityGrade(this float value)
        {
            return FromFloat(value);
        }

        /// <summary>
        /// Safe float to int conversion with rounding
        /// </summary>
        public static int ToIntSafe(this float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion with floor
        /// </summary>
        public static int ToIntFloor(this float value)
        {
            return Mathf.FloorToInt(value);
        }

        /// <summary>
        /// Safe float to int conversion with ceiling  
        /// </summary>
        public static int ToIntCeil(this float value)
        {
            return Mathf.CeilToInt(value);
        }

        /// <summary>
        /// Less than comparison for QualityGrade vs float
        /// </summary>
        public static bool IsLessThanFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() < value;
        }

        /// <summary>
        /// Greater than comparison for QualityGrade vs float
        /// </summary>
        public static bool IsGreaterThanFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() > value;
        }

        /// <summary>
        /// Less than or equal comparison for QualityGrade vs float
        /// </summary>
        public static bool IsLessThanOrEqualFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() <= value;
        }

        /// <summary>
        /// Greater than or equal comparison for QualityGrade vs float
        /// </summary>
        public static bool IsGreaterThanOrEqualFloat(this QualityGrade grade, float value)
        {
            return grade.ToFloat() >= value;
        }

        /// <summary>
        /// Equality comparison for QualityGrade vs float with tolerance
        /// </summary>
        public static bool IsEqualToFloat(this QualityGrade grade, float value, float tolerance = 0.01f)
        {
            return Mathf.Abs(grade.ToFloat() - value) <= tolerance;
        }

        /// <summary>
        /// Force explicit conversion from QualityGrade to float for problematic contexts
        /// </summary>
        public static float ForceFloat(this QualityGrade grade)
        {
            return (float)grade.ToFloat();
        }

        /// <summary>
        /// Force explicit conversion from float to QualityGrade for problematic contexts
        /// </summary>
        public static QualityGrade ForceQualityGrade(this float value)
        {
            return (QualityGrade)FromFloat(value);
        }

        /// <summary>
        /// Safe explicit conversion from float to int with validation
        /// </summary>
        public static int ToIntExplicit(this float value)
        {
            return (int)Mathf.Round(value);
        }

        /// <summary>
        /// Create QualityGrade from float with explicit cast
        /// </summary>
        public static QualityGrade CastToQualityGrade(float value)
        {
            return FromFloat(value);
        }

        /// <summary>
        /// Create float from QualityGrade with explicit cast
        /// </summary>
        public static float CastToFloat(QualityGrade grade)
        {
            return grade.ToFloat();
        }

        /// <summary>
        /// Helper methods for common conversion scenarios
        /// </summary>
        public static int ConvertFloatToInt(float value)
        {
            return Mathf.RoundToInt(value);
        }

        /// <summary>
        /// Find the best (highest) quality in a list
        /// </summary>
        public static QualityGrade BestQuality(this List<QualityGrade> qualities)
        {
            return qualities.Count > 0 ? qualities.Max() : QualityGrade.Poor;
        }
        
        /// <summary>
        /// Find the best quality from production data
        /// </summary>
        public static QualityGrade BestQuality(this List<PlantProductionRecord> production)
        {
            return production.Count > 0 ? production.Max(p => p.Quality) : QualityGrade.Poor;
        }

        /// <summary>
        /// Safe conversion with explicit float to int casting
        /// </summary>
        public static int SafeCastToInt(float value)
        {
            return (int)Mathf.Round(value);
        }

        /// <summary>
        /// Safe conversion with explicit QualityGrade to float casting
        /// </summary>
        public static float SafeCastToFloat(QualityGrade grade)
        {
            return (float)grade.ToFloat();
        }

        /// <summary>
        /// Force explicit int conversion for List.Count operations
        /// </summary>
        public static int ForceInt(this int value)
        {
            return value;
        }

        public static QualityGrade ConvertFloatToQuality(float value)
        {
            return FromFloat(value);
        }

        public static float ConvertQualityToFloat(QualityGrade grade)
        {
            return grade.ToFloat();
        }

        public static long ConvertQualityToLong(QualityGrade grade)
        {
            return grade.ToLong();
        }
    }
}