// ---------------------------------------------------------------------
// Copyright (c) 2014 John Atten
// http://www.codeproject.com/Articles/816301/Csharp-Building-a-Useful-Extensible-NET-Console-Ap
// ---------------------------------------------------------------------
namespace ConsoleShared
{
    public class ConsoleFormatting
    {
        public static string Indent(int count)
        {
            return "".PadLeft(count);
        }
        public static string FixedLength(string firstColumn, string secondColumn)
        {
            return $"{firstColumn,-60} {secondColumn}";
        }
    }
}
