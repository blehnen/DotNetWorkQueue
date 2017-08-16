// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace ConsoleShared
{
    public static class ConsoleParseArgument
    {
        public static object CoerceArgument(Type requiredType, string inputValue)
        {
            var isTimeSpan = false;
            var isArray = false;
            var requiredTypeCode = Type.GetTypeCode(requiredType);

            if (requiredTypeCode == TypeCode.Object)
            {
                var type = Nullable.GetUnderlyingType(requiredType);
                requiredTypeCode = Type.GetTypeCode(type);
                if (inputValue.ToLower() == "null")
                {
                    return null;
                }
                if (requiredTypeCode == TypeCode.Object)
                {
                    if (type == typeof (TimeSpan))
                    {
                        isTimeSpan = true;
                    }
                }
                else if (requiredType.BaseType != null && requiredType.BaseType == typeof(Array) && requiredType.UnderlyingSystemType.Name.Contains("TimeSpan[]"))
                { //a param array of timespan
                    isArray = true;
                    isTimeSpan = true;
                }
                else if (requiredType.BaseType != null && requiredType.BaseType == typeof(Array) && requiredType.UnderlyingSystemType.Name.Contains("String[]"))
                { //a param array of string
                    isArray = true;
                }
            }

            string exceptionMessage =
                $"Cannot coerce the input argument {inputValue} to required type {requiredType.Name}";

            object result;
            switch (requiredTypeCode)
            {
                case TypeCode.String:
                    result = inputValue;
                    break;
                case TypeCode.Int16:
                    short number16;
                    if (short.TryParse(inputValue, out number16))
                    {
                        result = number16;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int32:
                    int number32;
                    if (int.TryParse(inputValue, out number32))
                    {
                        result = number32;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int64:
                    long number64;
                    if (long.TryParse(inputValue, out number64))
                    {
                        result = number64;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Boolean:
                    bool trueFalse;
                    if (bool.TryParse(inputValue, out trueFalse))
                    {
                        result = trueFalse;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Byte:
                    byte byteValue;
                    if (byte.TryParse(inputValue, out byteValue))
                    {
                        result = byteValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Char:
                    char charValue;
                    if (char.TryParse(inputValue, out charValue))
                    {
                        result = charValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.DateTime:
                    DateTime dateValue;
                    if (DateTime.TryParse(inputValue, out dateValue))
                    {
                        result = dateValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Decimal:
                    decimal decimalValue;
                    if (decimal.TryParse(inputValue, out decimalValue))
                    {
                        result = decimalValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Double:
                    double doubleValue;
                    if (double.TryParse(inputValue, out doubleValue))
                    {
                        result = doubleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Single:
                    float singleValue;
                    if (float.TryParse(inputValue, out singleValue))
                    {
                        result = singleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt16:
                    ushort uInt16Value;
                    if (ushort.TryParse(inputValue, out uInt16Value))
                    {
                        result = uInt16Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt32:
                    uint uInt32Value;
                    if (uint.TryParse(inputValue, out uInt32Value))
                    {
                        result = uInt32Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt64:
                    ulong uInt64Value;
                    if (ulong.TryParse(inputValue, out uInt64Value))
                    {
                        result = uInt64Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Empty:
                    if (isTimeSpan && isArray)
                    {
                        var timespan = inputValue.Split(',');
                        var output = new List<TimeSpan>();
                        foreach (var ts in timespan)
                        {
                            TimeSpan timespanValue;
                            if (TimeSpan.TryParse(ts, out timespanValue))
                            {
                                output.Add(timespanValue);
                            }
                            else
                            {
                                throw new ArgumentException(exceptionMessage);
                            }
                        }
                        result = output.ToArray();
                    }
                    else if (isArray)
                    {
                        result = inputValue.Split(',');
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                default:
                    if (isTimeSpan)
                    {
                        TimeSpan timespanValue;
                        if (TimeSpan.TryParse(inputValue, out timespanValue))
                        {
                            result = timespanValue;
                        }
                        else
                        {
                            throw new ArgumentException(exceptionMessage);
                        }
                        break;
                    }
                    throw new ArgumentException(exceptionMessage);
            }
            return result;
        }
    }
}
