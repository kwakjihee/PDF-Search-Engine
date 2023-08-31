using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System.Diagnostics;
using static PDFSearchApp.MainWindow;

namespace PDFSearchApp
{
    public partial class MainWindow : Window
    {
        private List<string> searchHistory = new List<string>();
        private List<string> recentFiles = new List<string>();
        private ObservableCollection<PDFFile> PDFFiles { get; set; } = new ObservableCollection<PDFFile>();
        private ObservableCollection<PDFFile> FavoriteFiles { get; set; } = new ObservableCollection<PDFFile>();

        public MainWindow()
        {
            InitializeComponent();
            resultListView.ItemsSource = PDFFiles;
            LoadSearchHistory();
            LoadRecentFiles();
            LoadRecentFilesData();
            LoadFavoriteFilesData(); // Load favorite files data
            recentFilesListBox.ItemsSource = recentFiles.Select(file => new PDFFile
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file)
            }).Take(10);

            recentFilesListBox.MouseDoubleClick += RecentFilesListBox_MouseDoubleClick;
        }




        public partial class App : Application
        {
            protected override void OnStartup(StartupEventArgs e)
            {
                base.OnStartup(e);

                MainWindow mainWindow = new MainWindow();
                mainWindow.Show();
            }
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
                    AddToSearchHistory(searchKeyword);
                    SaveSearchHistory();

                    // 검색 결과를 업데이트한 후에 결과창을 보이게 하고 자동 완성 창은 숨깁니다.
                    resultListView.Visibility = Visibility.Visible;
                    autoCompletionListBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    MessageBox.Show("Please enter a search keyword and select a directory.");
                }
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


        private void TryOpenFile(string filePath)
        {
            if (FileHasBeenExecuted(filePath))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });

                    // 실행한 파일을 최근 파일 목록에 추가하고 저장
                    AddToRecentFilesList(filePath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private void AddToRecentFilesList(string filePath)
        {
            // 실행한 파일인지 확인 후 최근 파일 목록에 추가
            if (FileHasBeenExecuted(filePath) && !recentFiles.Contains(filePath))
            {
                recentFiles.Insert(0, filePath);
                if (recentFiles.Count > 10)
                {
                    recentFiles.RemoveAt(recentFiles.Count - 1);
                }

                // UI 업데이트 및 저장
                UpdateRecentFilesListBox();
                SaveRecentFilesData();
            }
        }


        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string searchKeyword = searchTextBox.Text;
            string directoryPath = directoryPathTextBox.Text;

            if (!string.IsNullOrEmpty(searchKeyword) && !string.IsNullOrEmpty(directoryPath))
            {
                SearchKeywordAndUpdateResults(searchKeyword, directoryPath);
                AddToSearchHistory(searchKeyword);
                SaveSearchHistory();

                // 검색 결과를 업데이트한 후에 결과 창을 보이도록 설정
                resultListView.Visibility = Visibility.Visible;
                recentFilesListBox.Visibility = Visibility.Collapsed; // 최근 파일 목록 숨기기
            }
            else
            {
                MessageBox.Show("Please enter a search keyword and select a directory.");
            }
        }


        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                var matchingHistory = searchHistory.Where(item => item.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                autoCompletionListBox.ItemsSource = matchingHistory;
                autoCompletionListBox.Visibility = Visibility.Visible;
            }
            else
            {
                autoCompletionListBox.ItemsSource = null; // Clear the item source
                autoCompletionListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void searchTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            autoCompletionListBox.Visibility = Visibility.Visible;
            UpdateAutoCompletionListBox();
        }

        private void UpdateAutoCompletionListBox()
        {
            string searchText = searchTextBox.Text;

            if (!string.IsNullOrEmpty(searchText))
            {
                var matchingHistory = searchHistory.Where(item => item.StartsWith(searchText, StringComparison.OrdinalIgnoreCase));
                autoCompletionListBox.ItemsSource = matchingHistory;
            }
            else
            {
                autoCompletionListBox.ItemsSource = null;
            }
        }

        private void UpdateRecentFilesListBox()
        {
            // 최근 파일 목록 ListBox에 최근 파일들을 바인딩
            recentFilesListBox.ItemsSource = recentFiles.Select(file => new PDFFile
            {
                FilePath = file,
                FileName = System.IO.Path.GetFileName(file) // 파일 이름만 표시
            }).Take(10);
        }

        private void AutoCompletionListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (autoCompletionListBox.SelectedItem is string selectedHistory)
            {
                searchTextBox.Text = selectedHistory;
                autoCompletionListBox.Visibility = Visibility.Collapsed;
            }
        }

        private void AddToSearchHistory(string searchKeyword)
        {

        }
        private void SaveRecentFiles()
        {
            using (FileStream fs = new FileStream("RecentFiles.dat", FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, recentFiles);
            }
        }
        private void SaveRecentFilesData()
        {
            RecentFilesData data = new RecentFilesData
            {
                RecentFiles = recentFiles
            };

            string dataFilePath = "RecentFilesData.dat";

            using (FileStream fs = new FileStream(dataFilePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, data);
            }
        }

        private void SaveSearchHistory()
        {
   
        }

        private void LoadSearchHistory()
        {
            if (File.Exists("SearchHistory.dat"))
            {
                using (FileStream fs = new FileStream("SearchHistory.dat", FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    searchHistory = (List<string>)formatter.Deserialize(fs);
                }
            }
        }
        private bool IsInDebugMode()
        {
#if DEBUG
            return true;
#else
    return false;
#endif
        }
        private bool FileHasBeenExecuted(string filePath)
        {
            // 파일이 실행된 적이 있는지 확인하는 로직을 구현
            // 예를 들어 파일이 존재하는지 확인하거나, 실행 로그를 확인할 수 있습니다.
            // 여기에서는 간단하게 파일이 존재하는지만 확인하도록 구현합니다.
            return File.Exists(filePath);
        }

        private void LoadRecentFiles()
        {
            string filePath = IsInDebugMode() ? "RecentFiles_Debug.dat" : "RecentFiles.dat";

            if (File.Exists(filePath))
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    recentFiles = (List<string>)formatter.Deserialize(fs);
                    // No need to reset recentFilesListBox.ItemsSource here
                }
            }
        }

        private void LoadRecentFilesData()
        {
            string dataFilePath = "RecentFilesData.dat";

            if (File.Exists(dataFilePath))
            {
                using (FileStream fs = new FileStream(dataFilePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    RecentFilesData data = (RecentFilesData)formatter.Deserialize(fs);
                    List<string> loadedRecentFiles = data.RecentFiles;

                    // 최근 실행한 파일 목록만 업데이트
                    recentFiles = loadedRecentFiles.Where(FileHasBeenExecuted).ToList();

                    // 최근 실행한 파일 목록을 UI에 바인딩하고 업데이트
                    UpdateRecentFilesListBox(); // Update ListBox UI with recent files

                    // No need to reassign recentFilesListBox.ItemsSource or save data here
                }
            }
        }


        private void RecentFilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (recentFilesListBox.SelectedItem is string selectedFilePath)
            {
                TryOpenFile(selectedFilePath);
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

        private void SelectDirectoryForFileDialogButton_Click(object sender, RoutedEventArgs e)
        {
            string selectedDirectory = GetSelectedDirectoryForFileDialog();

            if (!string.IsNullOrEmpty(selectedDirectory))
            {
                directoryPathTextBox.Text = selectedDirectory;
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

        private string GetSelectedDirectoryForFileDialog()
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
        private void ResultListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (resultListView.SelectedItem is PDFFile selectedPDF)
            {
                // Try to open the selected file
                TryOpenFile(selectedPDF.FilePath);

                // Check if the selected file was opened successfully
                if (FileHasBeenExecuted(selectedPDF.FilePath))
                {
                    // Add the selected file to recent files
                    recentFiles.Insert(0, selectedPDF.FilePath);
                    if (recentFiles.Count > 10)
                    {
                        recentFiles.RemoveAt(recentFiles.Count - 1);
                    }
                    UpdateRecentFilesListBox(); // Update recent files list

                    // Add the selected file to favorites
                    if (!FavoriteFiles.Contains(selectedPDF))
                    {
                        FavoriteFiles.Insert(0, selectedPDF);
                        if (FavoriteFiles.Count > 10)
                        {
                            FavoriteFiles.RemoveAt(FavoriteFiles.Count - 1);
                        }
                    }

                    SaveRecentFilesData(); // Save recent files history
                    SaveFavoriteFilesData(); // Save favorite files data
                }
            }
        }

        private void SaveFavoriteFilesData()
        {
            FavoriteFilesData data = new FavoriteFilesData
            {
                FavoriteFiles = FavoriteFiles.ToList()
            };

            string dataFilePath = "FavoriteFilesData.dat";

            using (FileStream fs = new FileStream(dataFilePath, FileMode.Create))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(fs, data);
            }
        }

        private void LoadFavoriteFilesData()
        {
            string dataFilePath = "FavoriteFilesData.dat";

            if (File.Exists(dataFilePath))
            {
                using (FileStream fs = new FileStream(dataFilePath, FileMode.Open))
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    FavoriteFilesData data = (FavoriteFilesData)formatter.Deserialize(fs);

                    // Update FavoriteFiles with loaded data
                    FavoriteFiles.Clear();
                    foreach (var file in data.FavoriteFiles)
                    {
                        FavoriteFiles.Add(file);
                    }
                }
            }
        }
        private void FavoritesButton_Click(object sender, RoutedEventArgs e)
        {
            if (recentFilesListBox.Visibility == Visibility.Visible)
            {
                recentFilesListBox.Visibility = Visibility.Collapsed; // Hide recent files list

                // Show favorite files list
                recentFilesListBox.ItemsSource = FavoriteFiles;
            }
            else
            {
                recentFilesListBox.Visibility = Visibility.Visible; // Show recent files list

                // Show recent files list
                recentFilesListBox.ItemsSource = recentFiles.Select(file => new PDFFile
                {
                    FilePath = file,
                    FileName = System.IO.Path.GetFileName(file) // 파일 이름만 표시
                }).Take(10);
            }
        }

        private void RecentFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (recentFilesListBox.SelectedItem is PDFFile selectedPDF)
            {
                TryOpenFile(selectedPDF.FilePath);
            }
        }


        [Serializable]
        public class PDFFile
        {
            public string? FileName { get; set; }
            public string? FilePath { get; set; }
            public string? AdditionalInfoBefore { get; set; }
            public string? Keyword { get; set; }
            public string? AdditionalInfoAfter { get; set; }
        }
    }
}
[Serializable]
public class RecentFilesData
{
    public List<string> RecentFiles { get; set; } = new List<string>();
}
[Serializable]
public class FavoriteFilesData
{
    public List<PDFFile> FavoriteFiles { get; set; } = new List<PDFFile>();
}

