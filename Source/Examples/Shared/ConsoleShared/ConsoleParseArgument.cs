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
                    if (type == typeof(TimeSpan))
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
                    if (short.TryParse(inputValue, out var number16))
                    {
                        result = number16;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int32:
                    if (int.TryParse(inputValue, out var number32))
                    {
                        result = number32;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Int64:
                    if (long.TryParse(inputValue, out var number64))
                    {
                        result = number64;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Boolean:
                    if (bool.TryParse(inputValue, out var trueFalse))
                    {
                        result = trueFalse;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Byte:
                    if (byte.TryParse(inputValue, out var byteValue))
                    {
                        result = byteValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Char:
                    if (char.TryParse(inputValue, out var charValue))
                    {
                        result = charValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.DateTime:
                    if (DateTime.TryParse(inputValue, out var dateValue))
                    {
                        result = dateValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Decimal:
                    if (decimal.TryParse(inputValue, out var decimalValue))
                    {
                        result = decimalValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Double:
                    if (double.TryParse(inputValue, out var doubleValue))
                    {
                        result = doubleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.Single:
                    if (float.TryParse(inputValue, out var singleValue))
                    {
                        result = singleValue;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt16:
                    if (ushort.TryParse(inputValue, out var uInt16Value))
                    {
                        result = uInt16Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt32:
                    if (uint.TryParse(inputValue, out var uInt32Value))
                    {
                        result = uInt32Value;
                    }
                    else
                    {
                        throw new ArgumentException(exceptionMessage);
                    }
                    break;
                case TypeCode.UInt64:
                    if (ulong.TryParse(inputValue, out var uInt64Value))
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
                            if (TimeSpan.TryParse(ts, out TimeSpan timespanValue))
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
                        if (TimeSpan.TryParse(inputValue, out TimeSpan timespanValue))
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
