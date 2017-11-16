using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    static class UsefulThings
    {
        public enum Alignments { LEFT, RIGHT }
        public const char PATH_SEPARATOR = '/';
        public const char DLETED_MARK = '$';

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
    }
}
