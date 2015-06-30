using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentinelDbUpdater
{
    static class PrintLogger
    {
        private const string path = @".\Log.txt";

        private static void WriteLine(string s, bool echo)
        {
            
            // This text is added only once to the file. 
            if (!File.Exists(path))
            {
                // Create a file to write to. 
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(s);
                }
            }

            // This text is always added, making the file longer over time 
            // if it is not deleted. 
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine(s);
            }
	
            if (echo) 
                Console.WriteLine(s);
        }

        public static void WriteLine(string s)
        {
            WriteLine(s, true);
        }

        public static string ReadLine()
        {

            var s = Console.ReadLine();
            // This text is added only once to the file. 
            WriteLine(s, false);
            return s;
        }


    }
}
