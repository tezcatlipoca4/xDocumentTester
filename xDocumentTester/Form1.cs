using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows.Forms;

namespace xDocumentTester
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
            {
                client.Encoding = Encoding.UTF8;
                string htmlCode = client.DownloadString("http://www.sport-fm.gr/tag/aris");

                StringReader reader = new StringReader(htmlCode);
                string line;
                int articlesFound = 0;
                bool foundStart = false;
                while ((line = reader.ReadLine()) != null)
                {
                    //Μέχρι να συναντήσουμε το <div class="row"> δεν κρατάμε τπτ
                    if (line.Contains("<div class=\"row\">"))
                        foundStart = true;

                    if (!foundStart) continue;

                    if (line.Contains("<a href=\"/article/") && line.Contains("title") && !line.Contains("</a>"))
                    {
                        int slashFoundIndex = line.IndexOf('/');
                        int titleFoundIndex = line.IndexOf("title");
                        int titleEndIndex = line.IndexOf("\">");
                        string filteredString = line.Substring(slashFoundIndex, titleFoundIndex - slashFoundIndex - 2);
                        richTextBox1.AppendText("url= http://www.sport-fm.gr" + filteredString + "\n");

                        filteredString = line.Substring(titleFoundIndex + 7, titleEndIndex - titleFoundIndex - 7);
                        richTextBox1.AppendText("title= " + filteredString + "\n");
                        articlesFound++;
                    }
                    else if (line.Contains("<span>"))

                    {
                        richTextBox1.AppendText("published at: " +
                                                line.Replace("<span>", "").Replace("</span>", "").TrimStart() + "\n");
                        if (articlesFound >= 15) return;
                    }
                    //Μόνο για το πρώτο άρθρο που έχει διαφορετική ρύθμιση για την ώρα!
                    else if (line.Contains("article-date") && articlesFound == 1)
                    {
                        int stringEndTextIndex = line.IndexOf("\">");
                        int dateEndIndex = line.IndexOf("</small>");
                        richTextBox1.AppendText("published at: " +
                                                line.Substring(stringEndTextIndex + 2,
                                                    dateEndIndex - stringEndTextIndex - 2).TrimStart() + "\n");
                    }
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
            using (WebClient client = new WebClient()) // WebClient class inherits IDisposable
            {
                client.Encoding = Encoding.UTF8;
                client.Headers.Add("User-Agent: Other");
                string htmlCode = client.DownloadString("http://www.sdna.gr/teams/paok").Replace("<div", "\n<div");
                StringReader reader = new StringReader(htmlCode);
                int articlesFound = 0;
                string line;


                //Το SDNA έχει όλες τις πληροφορίες σε μια γραμμή! Την εντοπίζουμε και την σπάμε σε σειρές ώστε να μπορέσουμε να πάρουμε τα άρθρα
                bool foundStart = false;
                while ((line = reader.ReadLine()) != null)
                {
                    //Μέχρι να συναντήσουμε το <div class="row"> που είναι και η μοναδική γραμμή που θέλουμε
                    if (line.Contains("<div class=\"external-wrapper\">"))
                        foundStart = true;

                    if (!foundStart) continue;

                    richTextBox1.AppendText(line + Environment.NewLine);
                }
            }
        }
    }
}