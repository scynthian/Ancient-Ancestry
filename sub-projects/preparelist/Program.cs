using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace preparelist
{
    class Program
    {
        static Dictionary<string, string> kitnames = new Dictionary<string, string>();
        static void Main(string[] args)
        {
            string ibd_dir = "ibd";
            //DEBUG
            //ibd_dir = @"D:\Genetics\Ancient-DNA\ibd";

            if (!Directory.Exists("ibd"))
            {
                Console.WriteLine("Folder 'ibd' or 'ref' does not exist!");
                return;
            }


            string[] files = Directory.GetFiles(ibd_dir);
            string kit = null;
            Dictionary<int, List<string>> dict = new Dictionary<int, List<string>>();
            List<string> _kits=null;
            string segments_str = null;
            foreach(string file in files)
            {
                kit = Path.GetFileNameWithoutExtension(file);
                //Console.WriteLine(kit);
                if (dict.ContainsKey(kit.Length))
                {
                    _kits = dict[kit.Length];
                    dict.Remove(kit.Length);
                }
                else
                    _kits = new List<string>();
                _kits.Add(kit);
                dict.Add(kit.Length, _kits);
            }
            var list = dict.Keys.ToList();
            list.Sort();
            list.Reverse();

            StringBuilder sb = new StringBuilder();

            foreach (var key in list)
            {
                //Console.WriteLine(key);
                foreach (string s in dict[key])
                {
                    segments_str = getSegments(ibd_dir + "\\" + s);
                    if (segments_str != "")
                        sb.Append(getName(s) + "," + s + "," + segments_str + "\r\n");
                    else
                        File.Delete(s);
                }
            }
            File.WriteAllText("atree.txt",sb.ToString());
            Console.WriteLine("atree.txt successfully written.");
           
        }

        private static string getSegments(string s)
        {
            string[] lines = File.ReadAllLines(s);
            string[] data = null;
            int chr = 0;
            int pos = 0;
            int pchr = 0;
            int ppos = 0;

            StringBuilder sb = new StringBuilder();
            int seg_start=0;
            int snp_count = 0;
            foreach(string l in lines)
            {
                if (l.Trim() == "")
                    continue;
                data = l.Split(new char[] { '\t' });
                chr = int.Parse(data[1]);
                pos = int.Parse(data[2]);
                snp_count++;

                if(seg_start==0)
                    seg_start=pos;

                if (pchr != chr && ppos - seg_start>50000)
                {
                    if (ppos != 0 && snp_count>=150)
                        sb.Append(" " + pchr + ":" + seg_start + ":" + ppos);
                    seg_start = 0;
                    snp_count = 0;
                }
                else if (pchr == chr && pos - ppos > 50000)
                {
                    if (ppos != 0 && snp_count >= 150)
                        sb.Append(" " + pchr + ":" + seg_start + ":" + ppos);
                    seg_start = 0;
                    snp_count = 0;
                }


                pchr = chr;
                ppos = pos;
            }
            if (sb.ToString().Trim().Replace(" ", "_")!="")
                return sb.ToString().Trim().Replace(" ", "_");
            else
            {
                string first = lines[0].Split(new char[]{'\t'})[1];
                string start = lines[0].Split(new char[] { '\t' })[2];
                string end = lines[lines.Length-1].Split(new char[] { '\t' })[2];
                string last = lines[lines.Length - 1].Split(new char[] { '\t' })[1];
                if(first==last && lines.Length>=150)
                    return first+":"+start+":"+end;
                else
                    return ""; // this shouldn't happen
            }
        }

        private static string getName(string s)
        {
            if (kitnames.Count == 0)
            {
                //kit-names.txt
                //load
                string[] lines = File.ReadAllLines("ref/kit-names.txt");
                string[] data = null;
                foreach(string line in lines)
                {
                    data=line.Split(new char[]{','});
                    kitnames.Add(data[0],data[1]);
                }
            }

            string[] kit_ids = s.Split(new char[] { '-' });
            StringBuilder sb = new StringBuilder();
            foreach(string kit_id in kit_ids)
            {
                sb.Append(kitnames[kit_id]);
                sb.Append("\t");
            }

            return sb.ToString().Trim().Replace("\t", "/").Replace("'", "&#39;");
        }
    }
}
