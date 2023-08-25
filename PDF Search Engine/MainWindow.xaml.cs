using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace PDFSearchApp
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<PDFFile> PDFFiles { get; set; } = new ObservableCollection<PDFFile>();

        public MainWindow()
        {
            InitializeComponent();
            resultListView.ItemsSource = PDFFiles;
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchKeyword = searchTextBox.Text;
            string directoryPath = directoryPathTextBox.Text;
            if (!string.IsNullOrEmpty(searchKeyword) && !string.IsNullOrEmpty(directoryPath))
            {
                SearchKeywordAndUpdateResults(searchKeyword, directoryPath);
            }
            else
            {
                MessageBox.Show("Please enter a search keyword and select a directory.");
            }
        }

        private void SearchKeywordAndUpdateResults(string searchKeyword, string directoryPath)
        {
            PDFFiles.Clear();

            if (Directory.Exists(directoryPath))
            {
                string[] pdfFiles = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.AllDirectories);

                foreach (string pdfFile in pdfFiles)
                {
                    if (SearchKeywordInPDF(pdfFile, searchKeyword, out string additionalInfoBefore, out string additionalInfoAfter))
                    {
                        PDFFiles.Add(new PDFFile { FileName = System.IO.Path.GetFileName(pdfFile), FilePath = pdfFile, AdditionalInfoBefore = additionalInfoBefore, Keyword = searchKeyword, AdditionalInfoAfter = additionalInfoAfter });
                    }
                }
            }
            else
            {
                MessageBox.Show("Selected directory does not exist.");
            }
        }

        private bool SearchKeywordInPDF(string pdfFilePath, string keyword, out string additionalInfoBefore, out string additionalInfoAfter)
        {
            additionalInfoBefore = string.Empty;
            additionalInfoAfter = string.Empty;

            using (var pdfReader = new PdfReader(pdfFilePath))
            {
                for (int pageIdx = 1; pageIdx <= pdfReader.NumberOfPages; pageIdx++)
                {
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfReader, pageIdx, strategy);

                    if (pageText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        (additionalInfoBefore, additionalInfoAfter) = GetContextAroundKeyword(pageText, keyword);
                        return true;
                    }
                }
            }
            return false;
        }

        private (string, string) GetContextAroundKeyword(string text, string keyword)
        {
            int maxContextWords = 2;

            string[] sentences = text.Split('.', '!', '?');
            foreach (var sentence in sentences)
            {
                if (sentence.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    string[] words = sentence.Split(' ');
                    for (int i = 0; i < words.Length; i++)
                    {
                        if (words[i].IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            int startIndex = Math.Max(0, i - maxContextWords);
                            int endIndex = Math.Min(words.Length - 1, i + maxContextWords);

                            string before = string.Join(" ", words, startIndex, i - startIndex);
                            string after = string.Join(" ", words, i + 1, endIndex - i);

                            return (before, after);
                        }
                    }
                }
            }
            return (string.Empty, string.Empty);
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchKeyword = searchTextBox.Text;
                string directoryPath = directoryPathTextBox.Text;
                if (!string.IsNullOrEmpty(searchKeyword) && !string.IsNullOrEmpty(directoryPath))
                {
                    SearchKeywordAndUpdateResults(searchKeyword, directoryPath);
                }
                else
                {
                    MessageBox.Show("Please enter a search keyword and select a directory.");
                }
            }
        }

        private void SelectDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedDirectory = GetSelectedDirectory();

            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                directoryPathTextBox.Text = selectedDirectory;
            }
        }

        private string GetSelectedDirectory()
        {
            var dialog = new CommonOpenFileDialog
            {
                IsFolderPicker = true
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                return dialog.FileName;
            }

            return string.Empty;
        }
    }

    public class PDFFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string AdditionalInfoBefore { get; set; }
        public string Keyword { get; set; }
        public string AdditionalInfoAfter { get; set; }
    }
}