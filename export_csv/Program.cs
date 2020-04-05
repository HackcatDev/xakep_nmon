using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace export_csv
{
    class Program
    {
        static void Main(string[] args)
        {
            String base_path = Path.Combine(Environment.CurrentDirectory, "snapshots");
            Console.WriteLine($"Loading snapshots from {base_path}...");
            Console.Write("Enter targer CSV file name: ");
            var fname = Console.ReadLine();
            Console.WriteLine("=====================================");
            String delim = ";";
            String tgtcsv = $"Snapshot time{delim}Internet up{delim}HTTP OK{delim}Average ping (ms){delim}Ping to router (ms){delim}Packet loss, %{delim}Measure ID{delim}SeqID{delim}STime{delim}{delim}AHR\r\n";
            List<net_state> snaps = new List<net_state>();
            int seq_id = 1;
            Console.Write("Enter how many days to process: ");
            int days = int.Parse(Console.ReadLine());
            if (days == 0) days = int.MaxValue;
            foreach (var cf in Directory.GetFiles(base_path, "*.json"))
            {
                FileInfo fi = new FileInfo(cf);
                if (DateTime.Now.Subtract(fi.CreationTime).TotalDays > days) continue;
                var ninfo = JsonConvert.DeserializeObject<net_state>(File.ReadAllText(cf));
                snaps.Add(ninfo);
            }
            //Now get host list
            Dictionary<String, int> host_ids = new Dictionary<string, int>();
            int hid = 0;
            foreach (var ci in snaps)
            {
                foreach (var ch in ci.avg_rtts.Keys)
                {
                    if (host_ids.ContainsKey(ch)) continue;
                    host_ids.Add(ch, hid++);
                }
            }
            //Build CSV header with this information
            string csv_hdrs = "";
            for (int i = 0; i < hid; i++)
            {
                csv_hdrs += $"RTT to {host_ids.Keys.ToArray()[i]}{delim}";
            }
            tgtcsv = tgtcsv.Replace("AHR", csv_hdrs);
            snaps.OrderBy(x => x.measure_time);
            snaps.Reverse();
            foreach (var ninfo in snaps)
            {
                tgtcsv +=
                    $"{ninfo.measure_time.ToShortDateString()} {ninfo.measure_time.ToShortTimeString()}{delim}" +
                    $"{ninfo.inet_ok}{delim}{ninfo.http_ok}{delim}{ninfo.avg_rtts.Values.Average().ToString("N2")}{delim}" +
                    ninfo.router_rtt.ToString() + delim +
                    (ninfo.packet_loss * 100).ToString("N2") + delim +
                    $"{ninfo.measure_id}{delim}{seq_id++}{delim}{ninfo.measure_time.ToShortTimeString()}{delim}{delim}";
                List<String> items = host_ids.Keys.ToList();
                foreach (var ci in items)
                {
                    if (ninfo.avg_rtts.ContainsKey(ci))
                    {
                        tgtcsv += ninfo.avg_rtts[ci];
                    }
                    tgtcsv += delim;
                }
                tgtcsv += $"{delim}\r\n";
            }
            File.WriteAllText(fname, tgtcsv);
            Console.WriteLine("OK");
            Console.ReadKey();
        }

    }
    struct net_state
    {
        public bool inet_ok;
        public bool http_ok;
        public Dictionary<String, int> avg_rtts;
        public double packet_loss;
        public DateTime measure_time;
        public int router_rtt;
        public long measure_id;
    }
}
