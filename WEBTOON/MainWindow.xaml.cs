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
using System.Globalization;

using Path = System.IO.Path;

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

        public async Task Search() //웹 요청 및 파싱
        {
            string keyword = tboxSearch.Text;
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"https://comic.naver.com/api/search/all?keyword={keyword}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            string jsonString = await response.Content.ReadAsStringAsync();
            JObject jsonObject = JObject.Parse(jsonString);

            var webtoonResults = jsonObject["searchWebtoonResult"]["searchViewList"]; // 웹툰
            var searchChallengeResults = jsonObject["searchChallengeResult"]["searchViewList"]; // 도전만화
            var searchBestChallengeResults = jsonObject["searchBestChallengeResult"]["searchViewList"]; // 베스트도전
            var comicResults = new JArray(webtoonResults.Concat(searchChallengeResults).Concat(searchBestChallengeResults)); // 웹툰 + 도전만화 + 베스트도전

            var comicResult = new List<searchList>();

            foreach (var item in comicResults)
            {
                int titleId = (int)item["titleId"];
                string titleName = (string)item["titleName"];
                string displayAuthor = (string)item["displayAuthor"];
                int articleTotalCount = (int)item["articleTotalCount"];
                string webtoonlevelcode = (string)item["webtoonLevelCode"];
                comicResult.Add(new searchList
                {
                    TitleId = titleId,
                    TitleName = titleName,
                    DisplayAuthor = displayAuthor,
                    ArticleTotalCount = articleTotalCount,
                    WebtoonLevelCode = webtoonlevelcode
                });
            }
            SearchListView.ItemsSource = comicResult;
        }

        public class searchList
        {
            public int TitleId { get; set; }
            public string TitleName { get; set; }
            public string DisplayAuthor { get; set; }
            public int ArticleTotalCount { get; set; }
            public string WebtoonLevelCode { get; set; }

            // 새로운 속성 추가
            public string DisplayArticleTotalCount
            {
                get
                {
                    return $"총 {ArticleTotalCount}화";
                }
            }
            public string DisplayWebtoonLevelCode
            {
                get
                {
                    if (WebtoonLevelCode == "WEBTOON")
                    {
                        return $"웹툰";
                    }
                    if (WebtoonLevelCode == "CHALLENGE")
                    {
                        return $"도전만화";
                    }
                    else //BEST_CHALLENGE
                    {
                        return $"베스트도전";
                    }
                }
            }
        }

        private void DownloadWebtoon(searchList selectedWebtoon, int no, string PathB)
        {
            string url = $"https://comic.naver.com/webtoon/detail?titleId={selectedWebtoon.TitleId}&no={no}";
            Dir.mkdir(PathB, selectedWebtoon.TitleName, no, url); // 경로, 제목, 화, URL
        }

        public class Dir // Directory 생성
        {
            public static void mkdir(string userpath, string title, int no, string url) // 경로, 제목, 화, URL
            {
                // 현재 실행 중인 MainWindow 인스턴스에 접근
                MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

                if (mainWindow != null) // MainWindow가 유효한지 확인
                {
                    if (mainWindow.chkSeparately.IsChecked == true)
                    {
                        string savepathS = userpath + "\\" + title + "\\" + no + "화"; // 예: C:\Users\Name\Desktop\유미의 세포들\n화
                        DirectoryInfo directoryinfo = Directory.CreateDirectory(savepathS);

                        MessageBox.Show("이미지 개별 다운로드를 시작합니다.");
                        mainWindow.GetImagesS(savepathS, url, no);
                    }
                    else if (mainWindow.chkSeparately.IsChecked == false)
                    {
                        string savepathM = userpath + "\\" + title; // 예: C:\Users\Name\Desktop\유미의 세포들
                        DirectoryInfo directoryinfo = Directory.CreateDirectory(savepathM);

                        MessageBox.Show("이미지 다중 다운로드를 시작합니다.");
                        mainWindow.GetImagesM(savepathM, url, no);
                    }
                }
                else
                {
                    MessageBox.Show("MainWindow 인스턴스를 찾을 수 없습니다.");
                }
            }
        }

        public async Task GetImagesS(string savepath, string url, int no)
        {
            Trace.WriteLine("개별 다운로드 모드");
            // 웹 요청
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla");
            var html = await httpClient.GetStringAsync(url);

            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);

            var comicViewArea = htmlDoc.DocumentNode.SelectSingleNode("//div[@id='comic_view_area']");

            if (comicViewArea != null)
            {
                var imgNodes = comicViewArea.SelectNodes(".//img");

                if (imgNodes != null)
                {
                    int imageIndex = 1;  // 이미지 번호를 저장하기 위한 변수
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
                                using (var memorystream = new MemoryStream(imageBytes))
                                {
                                    using (Bitmap bitmap = new Bitmap(memorystream))
                                    {
                                        // 개별 이미지 저장
                                        string imageFilePath = Path.Combine(savepath, $"{imageIndex}.jpg");
                                        bitmap.Save(imageFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
                                        imageIndex++;
                                    }
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
                }
            }
            else
            {
                MessageBox.Show("이미지를 다운로드할 수 없습니다.\n\n열람 시 로그인이 필요한 웹툰(유료 회차, 성인 웹툰 등)을 다운로드하려 했거나 존재하지 않는 화를 다운로드하려고 시도했을 가능성이 높습니다.");
            }
        }

        public async Task GetImagesM(string savepath, string url, int no)
        {
            Trace.WriteLine("다중 다운로드 모드");
            //웹 요청
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
                                using (var memorystream = new MemoryStream(imageBytes))
                                {
                                    Bitmap bitmap = new Bitmap(memorystream);
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
                        try
                        {
                            string outputFilePath = Path.Combine(savepath, $"{no}화.jpg");
                            CombineImages(images.ToArray(), outputFilePath);
                        }
                        catch
                        {
                            string outputFilePath = Path.Combine(savepath, $"{no}화.jpg");
                            FileInfo fileInfo = new FileInfo(outputFilePath); //오류 확인
                            if (fileInfo.Length == 0)
                            {
                                MessageBox.Show("오류가 발생했습니다.\n이미지가 너무 많아서 이미지 다중 다운로드에 실패했습니다.\n개별 다운로드를 진행해주세요.");
                                File.Delete(outputFilePath);
                            }
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("이미지를 다운로드할 수 없습니다.\n\n열람 시 로그인이 필요한 웹툰(유료 회차, 성인 웹툰 등)을 다운로드하려 했거나 존재하지 않는 화를 다운로드하려고 시도했을 가능성이 높습니다.");
            }
        }

        private void CombineImages(Bitmap[] images, string outputFilePath)
        {
            // 최종 이미지 크기 계산
            int width = images.Max(img => img.Width); // 가장 넓은 이미지의 폭으로 설정
            int height = images.Sum(img => img.Height); // 모든 이미지의 높이를 합산

            // 새로운 비트맵 생성
            using (var finalImage = new Bitmap(width, height))
            {
                using (Graphics graphics = Graphics.FromImage(finalImage))
                {
                    int offset = 0;
                    foreach (var img in images)
                    {
                        graphics.DrawImage(img, new System.Drawing.Rectangle(0, offset, img.Width, img.Height));
                        offset += img.Height;
                    }
                }
                //최종 이미지 저장
                finalImage.Save(outputFilePath, System.Drawing.Imaging.ImageFormat.Jpeg);
            }

            // 메모리 해제
            foreach (var img in images)
            {
                img.Dispose();
            }

            FileInfo fileInfo = new FileInfo(outputFilePath); //오류 확인
            if (fileInfo.Length == 0) 
            {
                MessageBox.Show("오류: 다중 저장에 실패하였습니다.\n개별 저장을 시도해주세요.");
            }
        }


        // 이벤트
        public void btnSearch_Click(object sender, RoutedEventArgs e) //필드 유효성 검사 후 Search()
        {
            if (string.IsNullOrWhiteSpace(tboxSearch.Text) || tboxSearch.Text == "제목/작가로 검색할 수 있습니다.")
            {
                MessageBox.Show("검색어를 입력해 주세요.");
                return;
            }
            Search();

        }

        private void btnPath_Click(object sender, RoutedEventArgs e) //Path 설정
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = "웹툰이 저장될 폴더를 선택하세요.";
            dialog.ShowDialog();
            tboxPath.Text = dialog.FolderName;
        }

        private void tboxSearch_GotFocus(object sender, RoutedEventArgs e) //placeholder 구현
        {
            tboxSearch.Text = "";
            
        }

        private void SearchListView_PreviewMouseDown(object sender, MouseButtonEventArgs e) //ListView 선택 구현
        {
            if (SearchListView.SelectedItem is searchList selectedWebtoon)
            {
                string noString = tboxNo.Text;
                string path = tboxPath.Text;
                if (string.IsNullOrWhiteSpace(noString)) // '저장할 화' 유효성 검사
                {
                    MessageBox.Show("저장할 화가 입력되어 있지 않습니다.");
                    return;
                }
                if (path == "설정되지 않음") // '이미지가 저장될 경로' 유효성 검사
                {
                    MessageBox.Show("이미지를 저장할 경로가 설정되지 않았습니다.");
                    return;
                }

                // 다중 다운로드 지원
                if (noString.Contains("~"))
                {
                    var parts = noString.Split('~');
                    if (parts.Length == 2 && int.TryParse(parts[0], out int startNo) && int.TryParse(parts[1], out int endNo))
                    {
                        // 입력된 범위에 따라 여러 화 다운로드
                        for (int dlno = startNo; dlno <= endNo; dlno++)
                        {
                            DownloadWebtoon(selectedWebtoon, dlno, path);
                        }
                    }
                    else
                    {
                        MessageBox.Show("잘못된 화 범위입니다. 올바른 입력의 예: 1~8");
                    }
                }
                else if (int.TryParse(noString, out int singleNo))
                {
                    // 단일 화 다운로드
                    DownloadWebtoon(selectedWebtoon, singleNo, path);
                }
                else
                {
                    MessageBox.Show("잘못된 입력입니다. 숫자 또는 범위를 입력하세요. 예: 1 또는 1~8");
                }
            }
        }

        private void tboxSearch_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            // your event handler here
            e.Handled = true;
            btnSearch_Click(sender, e);
        }
        private void tboxPath_GotFocus(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = "웹툰이 저장될 폴더를 선택하세요.";
            dialog.ShowDialog();
            tboxPath.Text = dialog.FolderName;
        }
    }
}