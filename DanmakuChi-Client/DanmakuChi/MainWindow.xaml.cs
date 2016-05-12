using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Web.Security;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.IO;
using WebSocketSharp;
using Newtonsoft.Json;

/*struct AiTuCaoMsg
{
    string type;
    string data;
}*/

namespace DanmakuChi {

    /// <summary>
    /// Interactive logic of MainWindow.xaml
    /// </summary>
    /// 

    public class AiTuCaoMsg
    {
        public string type;
        public string data;
    }

    public partial class MainWindow {
        public DanmakuCurtain dmkCurt;
        public Boolean isConnected = false;
        public WebSocket ws;
        private AiTuCaoMsg aiTuCaoMsg = null;
        private string recvBody = null;  // websocket接收内容
        private string qrCodeUrl = null; // 二维码url
        public Dictionary<string, string> emoji_map;//emoji映射
        public MainWindow() {
            try {
                InitializeComponent();

                // init
                AppendLog("Welcome to DanmakuChi CSharp Client!");
                //读取弹幕配置文件
                emoji_map = new Dictionary<string, string>();
                var con_moj = File.ReadAllText("../emoji_con");
                var input_moj = new StringReader(con_moj);
                string tmp;
                while ((tmp = input_moj.ReadLine()) != null)
                {
                    string[] sArray = tmp.Split(',');
                    emoji_map.Add(sArray[1], sArray[0]);
                }
                //chkShadow.IsChecked = config.Advanced.enableShadow;
                textServer.Text = "ws://192.168.191.1:8686";
                //textChannel.Text = "aitucao";
                aiTuCaoMsg = new AiTuCaoMsg();

            } catch (Exception e) {
                AppendLog(e.Message);
            }
        }

        public class Config {
            public Session Session { get; set; }
            public Wechat Wechat { get; set; }
            public Advanced Advanced { get; set; }
        }
        public class Session {
            public string server { get; set; }
            public string channel { get; set; }
        }
        public class Wechat {
            public string url { get; set; }
        }
        public class Advanced {
            public bool enableShadow { get; set; }
        }

        private void btnShowDmkCurt_Click(object sender, RoutedEventArgs e) {
            dmkCurt = new DanmakuCurtain(chkShadow.IsChecked.Value);
            dmkCurt.Show();
        }

        private void btnShotDmk_Click(object sender, RoutedEventArgs e) {

            if (dmkCurt != null)
            {
                dmkCurt.Hide();
                //dmkCurt.show();
            }
                
        }
        private void InitDanmaku() {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                dmkCurt = new DanmakuCurtain(chkShadow.IsChecked.Value);
                dmkCurt.Show();
                //isConnected = true;
                //btnConnect.IsEnabled = true;
                //btnConnect.Content = "Disconnect";
            }));
        }
        private void AppendLog(string text) {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                listLog.Items.Add("[" + DateTime.Now.ToString() + "] " + text);
                listLog.SelectedIndex = listLog.Items.Count - 1;
                listLog.ScrollIntoView(listLog.SelectedItem);
            }));
        }
        private void ShootDanmaku(string text,int type) {
            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
                dmkCurt.Shoot(text,type);
                AppendLog(text);
            }));
        }
        private void button_Click(object sender, RoutedEventArgs e) {

            if (!isConnected) {
                btnConnect.Content = "Connecting...";
                btnConnect.IsEnabled = false;

                var server = textServer.Text;
                var channel = textChannel.Text;

                //ws = new WebSocket(server + "/ws?channel=" + channel);
                ws = new WebSocket(server);
                ws.OnOpen += (s, ee) =>{
                    //AppendLog("connected!");
                    btnConnect.Content = "DisConnect";
                    btnConnect.IsEnabled = true;
                    isConnected = true;

                    aiTuCaoMsg.type = "CREATE_ROOM";
                    aiTuCaoMsg.data = textChannel.Text;
                    string json = JsonConvert.SerializeObject(aiTuCaoMsg);
                    ws.Send(json);
                };
                ws.OnMessage += (s, ee) => {
                    
                    Console.WriteLine(ee.Data.ToString());
                    recvBody = ee.Data.ToString();
                    // 反序列化Json的字符串
                    AiTuCaoMsg jsonRecvBody = JsonConvert.DeserializeObject<AiTuCaoMsg>(recvBody);

                    switch (jsonRecvBody.type) {
                        case "ROOM_ID": // 房间号
                            // 逻辑：为二维码的增加url，并初始化InitDanmuku()
                            qrCodeUrl = jsonRecvBody.data;
                            InitDanmaku();
                            break;
                        case "TEXT":
                            ShootDanmaku(jsonRecvBody.data, 0);
                            break;
                        case "EMOJ":
                            {
                                string tmp = "";
                                emoji_map.TryGetValue(jsonRecvBody.data, out tmp);//传进去"[鬼脸]"等emoji代码
                                string path = "../emoji/" + tmp;
                                ShootDanmaku(path, 1);
                                break;
                            }

                        case "PICTURE":
                            ShootDanmaku(jsonRecvBody.data, 2);
                            break;
                        default:
                            break;
                    }
                };
                ws.OnClose += (s, ee) => {
                    AppendLog("Disconnected!");
                };
                ws.Connect();
            } else {
                CancelDMK();
            }
            
        }
        private void CancelDMK() {
            ws.Close(); // 调用websocket结束
            btnConnect.Content = "Connect";
            isConnected = false;
            btnConnect.IsEnabled = true;
            if (dmkCurt != null) {
                dmkCurt.Close();
            }
        }
        private void SocketDotIO(object sender, DoWorkEventArgs e) {
            var server = ((string[])e.Argument)[0].ToString();
            var channel = ((string[])e.Argument)[1].ToString();
            var channelMd5 = FormsAuthentication.HashPasswordForStoringInConfigFile(channel, "MD5");

            var ws = new WebSocket(server + "/ws?channel=" + channel);
            ws.OnMessage += (s, ee) => {
                int dividerPos = ee.Data.IndexOf(':');
                string type = ee.Data.Substring(0, dividerPos);
                string body = ee.Data.Substring(dividerPos + 1);
                switch (type) {
                    case "INFO":
                        if (body == "OK") {
                            AppendLog("Successfully joined " + channel);
                            InitDanmaku();
                        } else {
                            AppendLog("Channel " + channel + " does not exist.");
                            CancelDMK();
                        }
                        break;
                    case "DANMAKU":
                        ShootDanmaku(body,1);//这个地方需要后台传递整型类型参数type，暂时设为1
                        break;
                }
            };
            ws.OnClose += (s, ee) => {
                AppendLog("DEAD");
            };
            ws.Connect();
        }

        private void Window_Closed(object sender, EventArgs e) {
            Application.Current.Shutdown();
        }

        private void btnQRCode_Click(object sender, RoutedEventArgs e) {
            // QRCode qrcode = new QRCode(textWechat.Text + "?dmk_channel=" + textChannel.Text, "Channel QRCode");
            QRCode qrcode = new QRCode(textWechat.Text + qrCodeUrl +":" + textChannel.Text, qrCodeUrl, "Channel QRCode");
            qrcode.Show();
        }

        private void chkShadow_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
}
