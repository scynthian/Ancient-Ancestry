using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ibdcsfast
{
    static class Program
    {
        static string out_folder = "ibd" + Path.DirectorySeparatorChar;
        static string data_files_folder = "data" + Path.DirectorySeparatorChar;

        static int snp_threshold = 150;

        static double base_pairs_threshold = 1000000;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //DEBUG
            //data_files_folder = @"D:\Genetics\Ancient-DNA\data\";
            //out_folder = @"D:\Genetics\Ancient-DNA\ibd\";
            

            Console.WriteLine("Genetic Genealogy Tools - Felix Chandrakumar <i@fc.id.au>");
            Console.WriteLine();
            //Console.WriteLine("Syntax:");
            //Console.WriteLine("\tibdcsfast.exe <base_pairs_threshold> <snps_threshold>");
            //Console.WriteLine("\r\nE.g., ibdcsfast.exe 1000000 150");
            //Console.WriteLine("data and ibd folder must exist.");

            if (args.Length == 2)
            {
                base_pairs_threshold = int.Parse(args[0]);
                snp_threshold = int.Parse(args[1]);
            }

            Console.WriteLine("data: " + data_files_folder);
            Console.WriteLine("ibd: " + out_folder);

            if (!Directory.Exists(data_files_folder) || !Directory.Exists(out_folder))
            {
                Console.WriteLine("Required data and ibd directories doesn't exist!");
                return;
            }

            string[] files = Directory.GetFiles(data_files_folder);
            int total = files.Length;

            foreach(string file in files)
            {
                ExecTask task = new ExecTask(data_files_folder, out_folder, snp_threshold, base_pairs_threshold);
                task.processFile(file);
            }
        }

       
    }
}
