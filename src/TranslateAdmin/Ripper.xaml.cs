using Azure.Data.Tables;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using TranslationTools;

namespace TranslateAdmin
{
    /// <summary>
    /// Interaction logic for Ripper.xaml
    /// </summary>
    public partial class Ripper : Window
    {
        ConsoleContent dc = new ConsoleContent();
        public Ripper()
        {
            InitializeComponent();
            DataContext = dc;
        }

        private void Ripper_start_Click(object sender, RoutedEventArgs e)
        {
            ImportButton.IsEnabled = false;
            //await RunRipper(PrefixText.Text, CountryFilter.Text);
            BackgroundWorker backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };
            backgroundWorker.DoWork += BackgroundWorker_DoWork;
            backgroundWorker.ProgressChanged += BackgroundWorker_ProgressChanged;
            backgroundWorker.RunWorkerAsync(argument: PrefixText.Text + "|" + CountryFilter.Text);
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            dc.Add((string)e.UserState);
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string Prefix = ((string)e.Argument).Split('|')[0];
            string Filter = ((string)e.Argument).Split('|')[1];
            RunRipper(sender, Prefix, Filter).GetAwaiter().GetResult();
        }

        private async Task RunRipper(object sender, string Prefix, string Filter)
        {
            BackgroundWorker worker = (BackgroundWorker)sender;
            String NextMarker = "";

            var client = new HttpClient();
            client.Timeout = new TimeSpan(0, 20, 0);

            do
            {
                worker.ReportProgress(0, string.Format("Download listings from bcartifacts ({0})", NextMarker));
                //dc.Add(string.Format("Download listings from bcartifacts ({0})", NextMarker));
                var Master = new XmlDocument();
                if (NextMarker == "")
                    Master.LoadXml(await client.GetStringAsync("https://bcartifacts.azureedge.net/sandbox/?comp=list&restype=container&prefix=" + Prefix));
                else
                    Master.LoadXml(await client.GetStringAsync("https://bcartifacts.azureedge.net/sandbox/?comp=list&restype=container&prefix=" + Prefix + "&marker=" + NextMarker));
                worker.ReportProgress(0, string.Format("- Download Done"));
                NextMarker = Master.GetElementsByTagName("NextMarker")[0].InnerText;
                foreach (XmlNode xn in Master.GetElementsByTagName("Blob"))
                {
                    var Name = xn.SelectSingleNode("Name").InnerText;
                    var country = Name.Substring(Name.IndexOf("/") + 1);
                    var vtxt = Name.Substring(0, Name.IndexOf("/"));
                    if (Filter.Length > 0)
                        if (!Name.Contains(Filter))
                            continue;
                    var URL = xn.SelectSingleNode("Url").InnerText;
                    worker.ReportProgress(0, string.Format("- Found {0} on {1}", Name, URL));
                    var data = await client.GetByteArrayAsync(URL);
                    MemoryStream ms = new MemoryStream(data.Length);
                    ms.Write(data);
                    ms.Position = 0;
                    worker.ReportProgress(0, "Unzipping");
                    ZipArchive zip = new ZipArchive(ms);
                    foreach (var entry in zip.Entries)
                    {
                        worker.ReportProgress(0, entry.FullName);
                        if (entry.FullName.EndsWith(".app"))
                        {
                            var col = GlobalVars.db.GetCollection<Translation>("translation");

                            var tableClient = new TableClient(new Uri("https://" + ConfigurationManager.AppSettings["storageaccount"] + ".table.core.windows.net"),
                                                "translation",
                                                new TableSharedKeyCredential(ConfigurationManager.AppSettings["storageaccount"], ConfigurationManager.AppSettings["storageaccountkey"]));

                            worker.ReportProgress(0, string.Format("- Extracting {0}", entry.Name));
                            var app = entry.Open();
                            MemoryStream m2 = new MemoryStream();
                            app.CopyTo(m2);
                            byte[] buf = m2.GetBuffer();
                            Buffer.BlockCopy(buf, 40, buf, 0, (int)m2.Length - 40);
                            m2.SetLength(m2.Length - 40);

                            ZipArchive appzip = new ZipArchive(m2);
                            string AppId = "";
                            string VersionTxt = "";
                            foreach (var appentry in appzip.Entries)
                            {
                                if (appentry.Name.Contains(".xlf") && !appentry.Name.Contains(".g.xlf"))
                                {
                                    worker.ReportProgress(0, string.Format("Processing translations {0}", System.IO.Path.GetFileName(appentry.Name)));
                                    MemoryStream TransStream = new MemoryStream();
                                    var translatetream = appentry.Open();
                                    translatetream.CopyTo(TransStream);
                                    TransStream.Seek(0, SeekOrigin.Begin);
                                    if (TransStream.Length > 20)
                                    {
                                        StreamReader sr = new StreamReader(TransStream);

                                        var model = XlfParser.Converter.Deserialize(sr.ReadToEnd());
                                        if (!ConfigurationManager.AppSettings["Languages"].Split(',').ToList().Contains(model.File.TargetLanguage))
                                        {
                                            worker.ReportProgress(0, string.Format(" - Skipping {0} translations in {1} file as the language is not included in .config key 'Languages'", model.File.Body.Group.TransUnit.Count, model.File.TargetLanguage));
                                        }
                                        else
                                        {
                                            worker.ReportProgress(0, string.Format(" - {0} translations in file", model.File.Body.Group.TransUnit.Count));
                                            int counter = 0;
                                            foreach (var Entry in model.File.Body.Group.TransUnit)
                                            {
                                                /*
                                                  <trans-unit id="Table 3759370895 - Property 2879900210" maxwidth="0" size-unit="char" translate="yes" xml:space="preserve">
                                                      <source>Account Entity Setup</source>
                                                      <target>Account Entity Setup</target>
                                                      <note from="Developer" annotates="general" priority="2"></note>
                                                      <note from="Xliff Generator" annotates="general" priority="3">Table Account Entity Setup - Property Caption</note>
                                                  </trans-unit>
                                                 */
                                                Translation StoreTranslation = new Translation();
                                                StoreTranslation.source = Entry.Source;
                                                StoreTranslation.target = Entry.Target;
                                                StoreTranslation.Language = model.File.TargetLanguage;
                                                StoreTranslation.Origin = entry.Name + " " + Name;
                                                StoreTranslation.Index = Translation.Hash(StoreTranslation.Language, StoreTranslation.source);
                                                if (!col.Exists(x => x.Index == StoreTranslation.Index))
                                                {
                                                    col.Insert(StoreTranslation);
                                                    col.EnsureIndex(x => x.Index);

                                                    var entity = new TableEntity(StoreTranslation.Language, StoreTranslation.Index)
                                                        {
                                                            { "Language", StoreTranslation.Language},
                                                            { "Origin", StoreTranslation.Origin },
                                                            { "Source", StoreTranslation.source },
                                                            { "Target", StoreTranslation.target }
                                                        };
                                                    try
                                                    {
                                                        tableClient.AddEntity(entity);
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        tableClient.UpdateEntity(entity, Azure.ETag.All);
                                                    }
                                                }
                                                counter++;
                                                if (counter % 1000 == 0)
                                                {
                                                    worker.ReportProgress(0, string.Format(" - {0} processed", counter));
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            } while (NextMarker != "");
            worker.ReportProgress(100, "Completed");
        }
    }
    public class ConsoleContent : INotifyPropertyChanged
    {
        string consoleInput = string.Empty;
        ObservableCollection<string> consoleOutput = new ObservableCollection<string>() { "Awaiting work..." };

        public ObservableCollection<string> ConsoleOutput
        {
            get
            {
                return consoleOutput;
            }
            set
            {
                consoleOutput = value;
                OnPropertyChanged("ConsoleOutput");
            }
        }

        public object Dispatcher { get; private set; }

        public void Add(string txt)
        {
            ConsoleOutput.Add(txt);
            System.Windows.Threading.Dispatcher.CurrentDispatcher.Invoke(new Action(() => { }), DispatcherPriority.ContextIdle);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        void OnPropertyChanged(string propertyName)
        {
            if (null != PropertyChanged)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
