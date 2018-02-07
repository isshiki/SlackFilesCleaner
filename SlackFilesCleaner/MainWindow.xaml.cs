using Codeplex.Data;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;


namespace SlackFilesCleaner
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        const string SLACK_FILES_LIST_URL = "https://slack.com/api/files.list?token={0}&count=1000&pretty=1";
        const string SLACK_FILES_DELETE_URL = "https://slack.com/api/files.delete?token={0}&file={1}&pretty=1";

        public MainWindow()
        {
            InitializeComponent();
        }

        private void buttonListFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ListFiles(this.textboxOAuthAccessToken.Text);
            }
            catch (Exception ex)
            {

                MessageBox.Show("リスト失敗： " + ex.Message);
            }
            
            MessageBox.Show("リスト完了！");
        }

        public void ListFiles(string oAuthAccessToken)
        {
            var methodURL = String.Format(SLACK_FILES_LIST_URL, oAuthAccessToken);

            var wc = new WebClient();
            var jsonString = wc.DownloadString(methodURL);

            var jsonObject = DynamicJson.Parse(jsonString);
            if (jsonObject.ok != true)
            {
                MessageBox.Show("エラーが起きているっぽい。：" + jsonObject.ToString());
                return;
            }

            var sbFilesList = new StringBuilder();
            foreach (var oneFile in jsonObject.files)
            {
                string id = "", timestamp = "", name = "";
                foreach (KeyValuePair<string, dynamic> item in oneFile)
                {
                    if (item.Key == "id")
                    {
                        id = item.Value;
                    }
                    else if (item.Key == "timestamp")
                    {
                        double seconds = item.Value;
                        timestamp = DateTimeFromUnixTimestampSeconds((long)seconds).ToString();
                    }
                    else if (item.Key == "name")
                    {
                        name = Regex.Unescape(item.Value);
                        sbFilesList.AppendLine($"{id} : {timestamp} : {name}");
                        continue;
                    }
                }
            }

            this.textboxFilesList.Text = sbFilesList.ToString();
        }

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        public static DateTime DateTimeFromUnixTimestampSeconds(long seconds)
        {
            return UnixEpoch.AddSeconds(seconds);
        }

        private void buttonDeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DeleteFiles(this.textboxOAuthAccessToken.Text);
            }
            catch (Exception ex)
            {

                MessageBox.Show("削除失敗： " + ex.Message);
            }

            MessageBox.Show("削除完了！");
        }

        public void DeleteFiles(string oAuthAccessToken)
        {
            var sbFilesList = new StringBuilder();
            var filesListText = this.textboxFilesList.Text;
            var filesArrayList = filesListText.Split('\n');
            foreach (var item in filesArrayList)
            {
                var itemsArrayList = item.Split(':');
                var id = itemsArrayList[0].Trim();
                if (String.IsNullOrEmpty(id)) continue;

                var methodURL = String.Format(SLACK_FILES_DELETE_URL, oAuthAccessToken, id);
                var wc = new WebClient();
                wc.Headers["content-type"] = "application/x-www-form-urlencoded";
                var jsonString = wc.UploadString(methodURL, "");

                var jsonObject = DynamicJson.Parse(jsonString);
                if (jsonObject.ok)
                {
                    sbFilesList.Append($"［削除成功］{item}");
                }
                else
                {
                    sbFilesList.Append($"【削除失敗】{item}");
                }
            }

            this.textboxFilesList.Text = sbFilesList.ToString();
        }
    }
}
