using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
                string htmlCode = client.DownloadString("http://www.sport-fm.gr/tag/paok");

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
                        line = line.Trim().Replace("<a href=\"", string.Empty);

                        int endUrlQuotreIndex = line.IndexOf('\"');

                        richTextBox1.AppendText("http://www.sport-fm.gr" + line.Substring(0, endUrlQuotreIndex - 1) + Environment.NewLine);

                        //Αφαιρούμε πλέον το url από τη γραμμή, το πρόθεμα του τίτλου και τα σύμβολα στο τέλος
                        line = line.Remove(0, endUrlQuotreIndex).Replace("\" title=\"", string.Empty).Replace("\">", string.Empty);

                        richTextBox1.AppendText(line + Environment.NewLine);
                        articlesFound++;
                    }
                    else if (line.Contains("<span>"))

                    {
                        richTextBox1.AppendText(line.Replace("<span>", "").Replace("</span>", "").TrimStart() + "\n");
                        if (articlesFound >= 15) return;
                    }
                    //Μόνο για το πρώτο άρθρο που έχει διαφορετική ρύθμιση για την ώρα!
                    else if (line.Contains("article-date") && articlesFound == 1)
                    {
                        line = line.Remove(0, line.IndexOf("\">") + 2).Replace("</small></h3>", string.Empty);
                        richTextBox1.AppendText(line + Environment.NewLine);
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
                //string htmlCode = client.DownloadString("http://www.sdna.gr/teams/paok").Replace("<div", "\n<div");
                string htmlCode = client.DownloadString("http://www.sdna.gr/teams/paok")
                    .Replace("><",
                        ">\n<"); //.Replace("<span class=\"field-content\">", "<span class=\"field-content\">\b");
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

                    //Έλεγχος για τις δυο περιπτώσεις ημερομηνίας
                    if (line.Contains("<em class=\"placeholder\">"))
                    {
                        //Το string περιέχει δεδομένα όπως 1 ωρα 11 λεπτά. Το μετατρέπουμε σε κανονική ημερομηνία
                        line = line.Replace("<span class=\"field-content\"> <em class=\"placeholder\">", "").Replace("</em> πριν </span>", "").Trim();

                        string[] splitString = line.Split(new[] { " " }, StringSplitOptions.None);
                        int secondsPassedFromPublish = 0;

                        //Μετατρεπουμε τα δεδομένα μας σε δευτερόλεπτα και βρίσκομε την ακριβή ώρα δημοσίευσης
                        for (int i = 0; i < splitString.Length; i++)
                        {
                            switch (splitString[i].ToLower())
                            {
                                case "ώρα":
                                case "ώρες":
                                    secondsPassedFromPublish += int.Parse(splitString[i - 1]) * 3600;
                                    break;
                                case "λεπτό":
                                case "λεπτά":
                                    secondsPassedFromPublish += int.Parse(splitString[i - 1]) * 60;
                                    break;
                                case "δευτ.":
                                    secondsPassedFromPublish += int.Parse(splitString[i - 1]);
                                    break;
                            }
                        }


                        richTextBox1.AppendText(DateTime.Now.AddSeconds(secondsPassedFromPublish * (-1)).ToString("g") + Environment.NewLine);
                    }

                    else if (line.Contains("<span class=\"field-content\">") && line.Contains("</span>"))
                    {
                        //Η σειρά μας ενδιαφέρει μόνο αν έχει ημερομηνία μέσα
                        string[] format = { "dd MMMM yyyy, HH:mm" };
                        line = line.Replace("<span class=\"field-content\">", "").Replace("</span>", "").TrimStart().TrimEnd();
                        DateTime retrievedDateTime;

                        if (DateTime.TryParseExact(line, format,
                            new CultureInfo("el-GR"),
                            //CultureInfo.CurrentCulture,
                            DateTimeStyles.AssumeLocal, out retrievedDateTime))
                        {
                            richTextBox1.AppendText(retrievedDateTime.ToString("g") + Environment.NewLine);
                        }

                    }
                    else if (!line.Contains("div class") && line.Contains("<a href=\"/"))
                    {
                        line = line.Replace("<a href=\"", string.Empty).Replace("</a>", string.Empty);

                        //Το σύμβολο (") χωρίζει το url από τον τίτλο
                        int breakSymbolIndex = line.IndexOf('\"');
                        string url = line.Substring(0, breakSymbolIndex);
                        richTextBox1.AppendText("www.sdna.gr" + url + Environment.NewLine);
                        
                        //Αφαιρούμε το url και τα διαχωριστικά (">) και μένει μόνο ο τίτλος του άρθρου
                        string title = line.Replace(url, string.Empty).Replace("\">",string.Empty);
                        richTextBox1.AppendText(title + Environment.NewLine);
                        articlesFound++;
                        
                    }

                    if (articlesFound >= 15) return;
                }
            }
        }
    }
}