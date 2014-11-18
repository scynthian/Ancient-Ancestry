using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace renkits
{
    class Program
    {
        static void Main(string[] args)
        {
            if(!Directory.Exists("ibd"))
            {
                Console.WriteLine("'ibd' folder does not exist!");
                return;
            }
            string[] files = Directory.GetFiles("ibd");
            foreach(string file in files)
            {
                string[] kits = Path.GetFileNameWithoutExtension(file).Split(new char[] { '-' });
                List<string> k = new List<string>();
                foreach(string kit in kits)
                    if(!k.Contains(kit))
                        k.Add(kit);
                StringBuilder sb = new StringBuilder();
                k.Sort();
                foreach (string kk in k)
                    sb.Append(" "+kk);
                string new_name = sb.ToString().Trim().Replace(" ", "-");
                new_name=Path.GetDirectoryName(file)+"\\"+new_name;
                Console.WriteLine(file + " -> " + new_name);
                if (file != new_name)
                {
                    if (File.Exists(new_name))
                        File.Delete(file);
                    else
                        File.Move(file, new_name);
                }               
            }
        }
    }
}
