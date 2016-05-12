using System;
using System.Collections.Generic;
using System.Drawing;
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
using System.Windows.Shapes;
using ThoughtWorks.QRCode.Codec;

namespace DanmakuChi {
    /// <summary>
    /// QRCode.xaml 的交互逻辑
    /// </summary>
    public partial class QRCode : Window {

        public QRCode(string data, string roomId, string title) {
            InitializeComponent();
            //textBox.Text = data;
            if(roomId != null && roomId.Length == 5)
            {
                // 设置label的逻辑 added by vincentsong 0512
                label1.Content = roomId.Substring(0, 1);
                label2.Content = roomId.Substring(1, 1);
                label3.Content = roomId.Substring(2, 1);
                label4.Content = roomId.Substring(3, 1);
                label5.Content = roomId.Substring(4, 1);
            }

            Title = title;

            var qrCodeEncoder = new QRCodeEncoder();
            qrCodeEncoder.QRCodeScale = 16;
            var image = qrCodeEncoder.Encode(data, Encoding.UTF8);
            var imageSrc = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
               image.GetHbitmap(),
               IntPtr.Zero,
               Int32Rect.Empty,
               BitmapSizeOptions.FromWidthAndHeight(image.Width, image.Height));
            imgBox.Source = imageSrc;
        }
    }
}
