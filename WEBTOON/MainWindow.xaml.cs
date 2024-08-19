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
using System.Drawing;


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
                int articleTotalCount = (int)item["articleTotalCount"];
                webtoonList.Add(new searchList
                {
                    TitleId = titleId,
                    TitleName = titleName,
                    DisplayAuthor = displayAuthor,
                    ArticleTotalCount = articleTotalCount
                });
            }
            SearchListView.ItemsSource = webtoonList;
        }

        public void btnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tboxSearch.Text) || tboxSearch.Text == "제목/작가로 검색할 수 있습니다.")
            {
                MessageBox.Show("검색어를 입력해 주세요.");
                return;
            }
                Search();

        }

        public class searchList
        {
            public int TitleId { get; set; }
            public string TitleName { get; set; }
            public string DisplayAuthor { get; set; }
            public int ArticleTotalCount { get; set; }

            // 새로운 속성 추가
            public string DisplayArticleTotalCount
            {
                get
                {
                    return $"총 {ArticleTotalCount}화";
                }
            }
        }

        /* private void CombineImages(string[] imagePaths, string outputFilePath)
        {
            // 모든 이미지를 불러오기
            List<Bitmap> images = new List<Bitmap>();
            foreach (var imagePath in imagePaths)
            {
                images.Add(new Bitmap(imagePath));
            }

            // 최종 이미지 크기 계산
            int width = images.Max(img => img.Width); // 가장 넓은 이미지의 폭으로 설정
            int height = images.Sum(img => img.Height); // 모든 이미지의 높이를 합산

            // 새로운 비트맵 생성
            using (var finalImage = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    int offset = 0;
                    foreach (var img in images)
                    {
                        g.DrawImage(img, new System.Drawing.Rectangle(0, offset, img.Width, img.Height));
                        offset += img.Height;
                    }
                }

                // 최종 이미지 저장
                finalImage.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            // 메모리 해제
            foreach (var img in images)
            {
                img.Dispose();
            }
        }
        */

        private void CombineImages(Bitmap[] images, string outputFilePath)
        {
            // 최종 이미지 크기 계산
            int width = images.Max(img => img.Width); // 가장 넓은 이미지의 폭으로 설정
            int height = images.Sum(img => img.Height); // 모든 이미지의 높이를 합산

            // 새로운 비트맵 생성
            using (var finalImage = new Bitmap(width, height))
            {
                using (Graphics g = Graphics.FromImage(finalImage))
                {
                    int offset = 0;
                    foreach (var img in images)
                    {
                        g.DrawImage(img, new System.Drawing.Rectangle(0, offset, img.Width, img.Height));
                        offset += img.Height;
                    }
                }

                // 최종 이미지 저장
                finalImage.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            // 메모리 해제
            foreach (var img in images)
            {
                img.Dispose();
            }
        }

        public class Save
        {
            public static void Dir(string path, string title, int no, string url) //경로, 제목, 화, URL
            {
                string pathA = path + "\\" + title;
                // Try to create the directory.
                DirectoryInfo di = Directory.CreateDirectory(pathA);
                MainWindow mainWindow = new MainWindow();
                mainWindow.GetImages(pathA, url, no);
            }
        }
        private void SearchListView_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (SearchListView.SelectedItem is searchList selectedWebtoon)
            {
                string noText = tboxNo.Text;
                string pathB = tboxPath.Text;
                if (string.IsNullOrWhiteSpace(noText))
                {
                    MessageBox.Show("저장할 화가 입력되어 있지 않습니다.");
                    return;
                }
                if (pathB == "설정되지 않음")
                {
                    MessageBox.Show("이미지를 저장할 경로가 설정되지 않았습니다.");
                    return;
                }

                // 화 범위를 처리하는 로직 추가
                if (noText.Contains("~"))
                {
                    var parts = noText.Split('~');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int startNo) && int.TryParse(parts[1], out int endNo))
                    {
                        // 입력된 범위에 따라 여러 화 다운로드
                        for (int no = startNo; no <= endNo; no++)
                        {
                            DownloadWebtoon(selectedWebtoon, no, pathB);
                        }
                    }
                    else
                    {
                        MessageBox.Show("잘못된 화 범위입니다. 예: 1~8");
                    }
                }
                else if (int.TryParse(noText, out int singleNo))
                {
                    // 단일 화 다운로드
                    DownloadWebtoon(selectedWebtoon, singleNo, pathB);
                }
                else
                {
                    MessageBox.Show("잘못된 입력입니다. 숫자 또는 범위를 입력하세요. 예: 1 또는 1~8");
                }
            }
        }

        private void DownloadWebtoon(searchList selectedWebtoon, int no, string PathB)
        {
            string url = $"https://comic.naver.com/webtoon/detail?titleId={selectedWebtoon.TitleId}&no={no}";
            Save.Dir(PathB, selectedWebtoon.TitleName, no, url); // 경로, 제목, 화, URL
        }

        /* public async Task GetImages(string pathA, string url)
        {
            string downloadPath = pathA;
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla");
            var html = await httpClient.GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            List<string> imageUrls = new List<string>();
            List<string> downloadedImagePaths = new List<string>();

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
                            downloadedImagePaths.Add(filePath);

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

                    // 이미지 모두 다운로드 후 합치기
                    if (downloadedImagePaths.Count > 0)
                    {
                        string outputFilePath = System.IO.Path.Combine(downloadPath, "combined.jpg");
                        CombineImages(downloadedImagePaths.ToArray(), outputFilePath);
                        MessageBox.Show($"모든 이미지를 합친 파일이 저장되었습니다: {outputFilePath}");
                    }
                }
            }
            MessageBox.Show("다운로드 완료");
        }
        */

        public async Task GetImages(string pathA, string url, int no)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla");
            var html = await httpClient.GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            List<Bitmap> images = new List<Bitmap>();

            var comicViewArea = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='comic_view_area']");

            if (comicViewArea != null)
            {
                var imgNodes = comicViewArea.SelectNodes(".//img");

                if (imgNodes != null)
                {
                    foreach (var imgNode in imgNodes)
                    {
                        string src = imgNode.GetAttributeValue("src", null);

                        if (!string.IsNullOrEmpty(src) && !src.Contains("white"))
                        {
                            Trace.WriteLine($"Found image: {src}");

                            try
                            {
                                // 이미지 데이터를 메모리로 다운로드
                                byte[] imageBytes = await httpClient.GetByteArrayAsync(src);
                                using (var ms = new MemoryStream(imageBytes))
                                {
                                    Bitmap bitmap = new Bitmap(ms);
                                    images.Add(bitmap);
                                }
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

                    // 이미지 결합 및 저장
                    if (images.Count > 0)
                    {
                        string outputFilePath = System.IO.Path.Combine(pathA, $"{no}화.jpg");
                        CombineImages(images.ToArray(), outputFilePath);
                    }
                }
            }
            //MessageBox.Show("다운로드 완료");
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