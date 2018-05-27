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


                        richTextBox1.AppendText("Published at: " + DateTime.Now.AddSeconds(secondsPassedFromPublish * (-1)).ToString("g") + Environment.NewLine);

                        //   MessageBox.Show(line);
                    }
                    else if (line.Contains("<span class=\"field-content\">") && line.Contains("</span>"))
                    {
                        //Η σειρά μας ενδιαφέρει μόνο αν έχει ημερομηνία μέσα
                        string[] format = { "dd MMMM yyyy, HH:mm" };
                        string tempLineData = line.Replace("<span class=\"field-content\">", "").Replace("</span>", "").TrimStart().TrimEnd();
                        DateTime retrievedDateTime;

                        if (DateTime.TryParseExact(tempLineData, format,
                            new CultureInfo("el-GR"),
                            //CultureInfo.CurrentCulture,
                            DateTimeStyles.AssumeLocal, out retrievedDateTime))
                        {
                            richTextBox1.AppendText("Published at:" + tempLineData + Environment.NewLine);
                        }

                    }
                    else if (!line.Contains("div class") && line.Contains("<a href=\"/"))
                    {
                        line = line.Replace("<a href=\"/", string.Empty).Replace("</a>", string.Empty);
                        //Το σύμβολο > χωρίζει το url από τον τίτλο
                        int breakSymbolIndex = line.IndexOf('>');
                        string url = "url = www.sdna.gr/" + line.Substring(1, breakSymbolIndex - 2);
                        string title = "Τίτλος: " + line.Substring(breakSymbolIndex + 1, line.Length - 1 - breakSymbolIndex);

                        richTextBox1.AppendText(url + Environment.NewLine);
                        richTextBox1.AppendText(title + Environment.NewLine);
                        articlesFound++;
                        //MessageBox.Show(line);
                    }

                    if (articlesFound >= 15) return;
                }
            }
        }
    }
}