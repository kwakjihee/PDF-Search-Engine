using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Windows;
using System.Windows.Input;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

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
            SearchKeywordAndUpdateResults(searchKeyword);
        }

        private void SearchKeywordAndUpdateResults(string searchKeyword)
        {
            PDFFiles.Clear();

            string directoryPath = @"C:\Users\klob\Desktop\곽지희";
            string[] pdfFiles = Directory.GetFiles(directoryPath, "*.pdf", SearchOption.AllDirectories);

            foreach (string pdfFile in pdfFiles)
            {
                if (SearchKeywordInPDF(pdfFile, searchKeyword))
                {
                    PDFFiles.Add(new PDFFile { FileName = System.IO.Path.GetFileName(pdfFile), FilePath = pdfFile });
                }
            }
        }

        private bool SearchKeywordInPDF(string pdfFilePath, string keyword)
        {
            using (var pdfReader = new PdfReader(pdfFilePath))
            {
                for (int pageIdx = 1; pageIdx <= pdfReader.NumberOfPages; pageIdx++)
                {
                    var pageText = PdfTextExtractor.GetTextFromPage(pdfReader, pageIdx);
                    if (pageText.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchKeyword = searchTextBox.Text;
                SearchKeywordAndUpdateResults(searchKeyword);
            }
        }
    }

    public class PDFFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }
}