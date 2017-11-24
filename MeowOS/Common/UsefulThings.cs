using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MeowOS
{
    static class UsefulThings
    {
        public enum Alignments { LEFT, RIGHT }
        public const char PATH_SEPARATOR = '/';
        public const char USERDATA_SEPARATOR = '|';
        public const char DELETED_MARK = '$';
        public const string EOLN_STR = "\r\n";
        public static readonly Encoding ENCODING = Encoding.GetEncoding(1251);
        public static readonly byte[] EOLN_BYTES = ENCODING.GetBytes(EOLN_STR);

        public static string setStringLength(string input, int maxLength, char placeholder = '\0', Alignments alignment = Alignments.LEFT)
        {
            if (input.Length == maxLength)
                return input;

            string output = new string(placeholder, maxLength);
            if (input.Length > maxLength)
                output = input.Substring(0, maxLength);
            else
            {
                output = output.Remove(0, input.Length);
                switch (alignment)
                {
                    case Alignments.LEFT:
                        output = input + output;
                        break;
                    case Alignments.RIGHT:
                        output = output + input;
                        break;
                }
            }
            return output;
        }

        public static string truncateZeros(string input)
        {
            int indexOfZero = input.IndexOf('\0');
            if (indexOfZero < 0)
                return input;
            else
                return input.Substring(0, indexOfZero);
        }

        public static string clearExcessSeparators(string path)
        {
            if (path == null)
                return null;

            string[] parts = path.Split(PATH_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
            string result = "";
            for (int i = 0; i < parts.Length; ++i)
                result += (PATH_SEPARATOR + parts[i]);
            return result;
        }

        public static void detachLastFilename(string path, out string pathWithoutLast, out string last)
        {
            if (path == null)
            {
                pathWithoutLast = null;
                last = null;
            }
            else if (path.IndexOf(PATH_SEPARATOR) < 0)
            {
                pathWithoutLast = "";
                last = path;
            }
            else
            {
                pathWithoutLast = path.Remove(path.LastIndexOf(PATH_SEPARATOR));
                last = path.Substring(path.LastIndexOf(PATH_SEPARATOR) + 1);
            }
        }

        public static void controlLettersAndDigits(TextBox tb)
        {
            int ss = tb.SelectionStart - 1;
            string text = tb.Text;
            tb.Text = string.Concat(tb.Text.Where(char.IsLetterOrDigit));

            if (!tb.Text.Equals(text))
                tb.SelectionStart = Math.Max(0, ss);
        }

        public static string readLine(byte[] data)
        {
            return ENCODING.GetString(data.Take(Array.IndexOf(data, EOLN_BYTES.First())).ToArray());
        }

        public static byte[] skipLine(byte[] data)
        {
            return data.Skip(Array.IndexOf(data, EOLN_BYTES.Last()) + 1).ToArray();
        }

        public static string[] fileFromByteArrToStringArr(byte[] input)
        {
            string buf = ENCODING.GetString(input);
            string[] output = buf.Split(new string[] { EOLN_STR }, StringSplitOptions.None);
            int i;
            for (i = output.Length - 1; i >= 0 && output[i].Length == 0; --i);
            return output.Take(i + 1).ToArray();
        }

        public static string replaceControlChars(string input)
        {
            return input.Replace('|', 'x').Replace('\r', 's').Replace('\n', 'f');
        }
    }
}
