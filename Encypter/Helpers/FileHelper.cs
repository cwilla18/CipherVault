using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Encypter.Helpers;
internal static class FileHelper
{
    public static bool ValidateFileExists(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            Console.WriteLine("File path cannot be empty.");
            return false;
        }
        if (!Directory.Exists(filePath))
        {
            Console.WriteLine("The specified directory does not exist.");
            return false;
        }
        return true;
    }
}
