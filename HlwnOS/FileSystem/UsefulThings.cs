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
    }
}
