using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json.Linq;
using HtmlAgilityPack;
using System.IO;
using System;

namespace WEBTOON
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public async Task Search()
        {
            string keyword = tboxSearch.Text;
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://comic.naver.com/api/search/all?keyword={keyword}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(jsonString);

            var webtoonResults = jsonObject["searchWebtoonResult"]["searchViewList"];
            var webtoonList = new List<searchList>();

            foreach (var item in webtoonResults)
            {
                int titleId = (int)item["titleId"];
                string titleName = (string)item["titleName"];
                string displayAuthor = (string)item["displayAuthor"];
                Trace.WriteLine($"Title ID: {titleId}, Title Name: {titleName}, Display Author: {displayAuthor}");
                webtoonList.Add(new searchList
                {
                    TitleId = titleId,
                    TitleName = titleName,
                    DisplayAuthor = displayAuthor
                });
            }
            SearchListView.ItemsSource = webtoonList;
        }

        public void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            Search();

        }

        public class searchList
        {
            public int TitleId { get; set; }
            public string TitleName { get; set; }
            public string DisplayAuthor { get; set; }
        }

        public class Save
        {
            public static void Dir(string path, string title, int no, string url) //경로, 제목, 화, URL
            {
                string pathA = path + "\\" + title + "\\" + no + "화";
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(pathA);
                MainWindow mainWindow = new MainWindow();
                mainWindow.GetImages(pathA, url);
            }
        }
        private void SearchListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchListView.SelectedItem is searchList selectedWebtoon)
            {
                if (tboxNo.Text == "")
                {
                    MessageBox.Show("저장할 화를 입력해주세요.");
                    return;
                }
                int no = int.Parse(tboxNo.Text);
                MessageBox.Show($"선택된 웹툰: {selectedWebtoon.TitleName} - {selectedWebtoon.DisplayAuthor}");
                string url = $"https://comic.naver.com/webtoon/detail?titleId={selectedWebtoon.TitleId}&no={no}";
                Save.Dir(tboxPath.Text, selectedWebtoon.TitleName, no, url); //경로, 제목, 화, URL
            }
        }

        public async Task GetImages(string pathA, string url)
        {
            string downloadPath = pathA;
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla");
            Trace.WriteLine(url);
            var html = await httpClient.GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            List<string> imageUrls = new List<string>();

            var comicViewArea = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='comic_view_area']");

            if (comicViewArea != null)
            {
                var imgNodes = comicViewArea.SelectNodes(".//img");

                if (imgNodes != null)
                {
                    int imageIndex = 1;

                    foreach (var imgNode in imgNodes)
                    {
                        string src = imgNode.GetAttributeValue("src", null);

                        if (!string.IsNullOrEmpty(src) && !src.Contains("white"))
                        {
                            imageUrls.Add(src);
                            Trace.WriteLine($"Found image: {src}");

                            string fileName = $"{imageIndex}.jpg";
                            string filePath = System.IO.Path.Combine(downloadPath, fileName);

                            try
                            {
                                byte[] imageBytes = await httpClient.GetByteArrayAsync(src);
                                await File.WriteAllBytesAsync(filePath, imageBytes);
                                imageIndex++;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show($"Failed to download {src}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Trace.WriteLine($"Skipped image: {src} (contains 'white')");
                        }
                    }
                }
            }
            MessageBox.Show("다운로드 완료");
        }

        private void btnPath_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = "웹툰이 저장될 폴더를 선택하세요.";
            dialog.ShowDialog();
            tboxPath.Text = dialog.FolderName;
        }

        private void tboxSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            tboxSearch.Text = "";
        }

        
    }
}