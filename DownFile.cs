using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CreateIISSite
{
    class DownFile
    {
        #region Property

        public string uriString;// = this.txtDownUrl.Text.Trim();
        public string userName;//= this.txtUsername.Text.Trim();
        public string password;// = this.txtUserpwd.Password.Trim();
        public string domain;//= this.txtDomain.Text.Trim();
        WebClient client;
        List<string> fileList;
        #endregion

        public void Analysis()
        {
            client = new WebClient();
            client.BaseAddress = uriString;
            NetworkCredential credential = new NetworkCredential(userName, password, domain);
            client.Credentials = credential;
            byte[] bytes = client.DownloadData(new Uri(uriString));
            string sHtmlText = Encoding.ASCII.GetString(bytes);
            fileList = this.GetFileList(sHtmlText);

        }
        public List<string> GetFileList(string sHtmlText)
        {
            List<string> list = new List<string>();
            bool flag = false;
            string str = DateTime.Now.ToShortDateString();
            while (!flag)
            {
                Regex regex = new Regex("<a\\S*\\Dhref=\\\"(.*)\\\">(.*)</a>", RegexOptions.IgnoreCase);
                string input = sHtmlText;
                if (sHtmlText.Contains(str))
                {
                    input = sHtmlText.Remove(0, sHtmlText.IndexOf(str));
                    MatchCollection matchs = new Regex(@"<A\s+[^>]*\s*HREF\s*=\s*([']?)(?<url>\S+)'?[^>]*</A>").Matches(input);
                    foreach (Match match in matchs)
                    {
                        if (match.Length != 0)
                        {
                            string item = match.Result("${url}");
                            item = item.Remove(0, item.IndexOf(">")).Replace("</A>", "").Replace("</a>", "").Replace("br", "").Replace("<", "").Replace(">", "");
                            if (item.EndsWith(".txt")) continue;
                            list.Add(item);
                        }
                    }
                }
                flag = true;
            }
            return list;
        }


    }
}
