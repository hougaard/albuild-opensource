using Azure.Data.Tables;
using LiteDB;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TranslationTools;

namespace TranslateAdmin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Languages = ConfigurationManager.AppSettings["Languages"].Split(',').ToList();
            Languages.Insert(0, "Any");
            LanguageSelect.ItemsSource = Languages;
            LanguageSelect.SelectedIndex = 0;
            GlobalVars.db = new LiteDatabase(ConfigurationManager.AppSettings["Database"]);
        }
        ~MainWindow()
        {
            GlobalVars.db.Dispose();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var col = GlobalVars.db.GetCollection<Translation>("translation");
            if (SearchName.Text.StartsWith("*"))
            {
                if (LanguageSelect.SelectedIndex == 0)
                {
                    var results = col.Query().Where(x => x.source.Contains(SearchName.Text.Substring(1))).Limit(200).ToList();
                    Result.ItemsSource = results;
                }
                else
                {
                    var results = col.Query().Where(x => x.source.Contains(SearchName.Text.Substring(1)) && x.Language == (string)LanguageSelect.SelectedItem).Limit(200).ToList();
                    Result.ItemsSource = results;
                }
            }
            else
            {
                if (LanguageSelect.SelectedIndex == 0)
                {
                    var results = col.Query().Where(x => x.source.Equals(SearchName.Text)).Limit(200).ToList();
                    Result.ItemsSource = results;
                }
                else
                {
                    var results = col.Query().Where(x => x.source.Equals(SearchName.Text) && x.Language == (string)LanguageSelect.SelectedItem).Limit(200).ToList();
                    Result.ItemsSource = results;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            LoadedTranslation.source = source.Text;
            LoadedTranslation.target = target.Text;
            LoadedTranslation.Origin = Origin.Text;
            LoadedTranslation.Language = language.Text;

            var col = GlobalVars.db.GetCollection<Translation>("translation");
            col.Update(LoadedTranslation);

            var tableClient = new TableClient(new Uri("https://" + ConfigurationManager.AppSettings["storageaccount"] + ".table.core.windows.net"),
                                                  "translation",
                                                  new TableSharedKeyCredential(ConfigurationManager.AppSettings["storageaccount"], ConfigurationManager.AppSettings["storageaccountkey"]));


            var entity = new TableEntity(LoadedTranslation.Language, LoadedTranslation.Index)
                                                    {
                                                        { "Language", LoadedTranslation.Language},
                                                        { "Origin", LoadedTranslation.Origin },
                                                        { "Source", LoadedTranslation.source },
                                                        { "Target", LoadedTranslation.target }
                                                    };
            try
            {
                tableClient.AddEntity(entity);
            }
            catch (Exception ex)
            {
                tableClient.UpdateEntity(entity, Azure.ETag.All);
            }

            LoadedTranslation = null;
            source.Text = "";
            target.Text = "";
            Origin.Text = "";
            language.Text = "";
            Save.IsEnabled = false;
            Cancel.IsEnabled = false;
            SearchButton.IsEnabled = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            LoadedTranslation = null;
            source.Text = "";
            target.Text = "";
            Origin.Text = "";
            language.Text = "";
            Save.IsEnabled = false;
            Cancel.IsEnabled = false;
            SearchButton.IsEnabled = true;
        }

        private void Result_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox lb = (ListBox)sender;
            LoadedTranslation = (Translation)lb.SelectedItem;
            source.Text = LoadedTranslation.source;
            target.Text = LoadedTranslation.target;
            Origin.Text = LoadedTranslation.Origin + "+ Edited";
            language.Text = LoadedTranslation.Language;
            Save.IsEnabled = true;
            Cancel.IsEnabled = true;
            SearchButton.IsEnabled = false;
        }
        Translation LoadedTranslation;
        List<string> Languages;

        private void ImportXLFMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Translation files (*.xlf)|*.xlf";
            if (openFileDialog.ShowDialog() == true)
                ImportXLF(openFileDialog.FileName);
        }

        private void Ripper_Click(object sender, RoutedEventArgs e)
        {
            Ripper ripWindow = new Ripper();
            ripWindow.Show();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        static void ImportXLF(string FileName)
        {
            var tableClient = new TableClient(new Uri("https://" + ConfigurationManager.AppSettings["storageaccount"] + ".table.core.windows.net"),
                                                      "translation",
                                                      new TableSharedKeyCredential(ConfigurationManager.AppSettings["storageaccount"], ConfigurationManager.AppSettings["storageaccountkey"]));


            var col = GlobalVars.db.GetCollection<Translation>("translation");

            var model = XlfParser.Converter.Deserialize(File.ReadAllText(FileName));
            foreach (var Entry in model.File.Body.Group.TransUnit)
            {
                Translation StoreTranslation = new Translation();
                StoreTranslation.source = Entry.Source;
                StoreTranslation.target = Entry.Target;
                StoreTranslation.Language = model.File.TargetLanguage;
                StoreTranslation.Origin = System.IO.Path.GetFileName(FileName);
                StoreTranslation.Index = Translation.Hash(StoreTranslation.Language, StoreTranslation.source);
                if (!col.Exists(x => x.Index == StoreTranslation.Index))
                {
                    col.Insert(StoreTranslation);
                    col.EnsureIndex(x => x.Index);
                }
                else
                {
                    col.Update(StoreTranslation);
                    col.EnsureIndex(x => x.Index);
                }

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
                catch
                {
                    tableClient.UpdateEntity(entity, Azure.ETag.All);
                }
                MessageBox.Show("Done");
            }
        }
    }
}
