using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using ZXing;
using ZXing.QrCode;
using ZXing.QrCode.Internal;
using Color = System.Windows.Media.Color;
using Image = System.Windows.Controls.Image;

namespace Google_Authenticator_Backup_Tool
{
	public class GAItem
	{
		public string email { set; get; }
		public string issuer { set; get; }
		public string secret { set; get; }
		public string url { set; get; }
		public ImageSource image { set; get; }
	}
	
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		List<GAItem> items = new List<GAItem>();

		public MainWindow()
		{
			InitializeComponent();
		}

		public void test()
		{
			
		}

		private void Button_Click(object sender, RoutedEventArgs e)
		{
			lstOperations.Items.Clear();
			items.Clear();

			System.Diagnostics.Process process = new System.Diagnostics.Process();
			System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
			startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
			startInfo.FileName = "cmd.exe";
			startInfo.RedirectStandardInput = true;
			startInfo.UseShellExecute = false;
			process.StartInfo = startInfo;

			
			process.Start();

			using (StreamWriter sw = process.StandardInput)
			{
				if (sw.BaseStream.CanWrite)
				{
					lstOperations.Items.Add("Create Temp Folder");
					sw.WriteLine("mkdir authenticator-backup");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("cd \"authenticator-backup\"");

					lstOperations.Items.Add("Elevating root access");
					sw.WriteLine("adb shell su");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("cp /data/data/com.google.android.apps.authenticator2/databases/databases /data/local/tmp");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("chown shell.shell /data/local/tmp/databases");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("exit");
					System.Threading.Thread.Sleep(500);

					lstOperations.Items.Add("Pull Google Authenticator DB");
					sw.WriteLine("adb pull /data/local/tmp/databases");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("adb shell su");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("rm /data/local/tmp/databases");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("exit");
					System.Threading.Thread.Sleep(100);

					lstOperations.Items.Add("Extracting Data");
					sw.WriteLine("sqlite3 ./databases \"select * from accounts\"");
					System.Threading.Thread.Sleep(100);

					using (SQLiteConnection connection = new SQLiteConnection("Data Source="+ AppDomain.CurrentDomain.BaseDirectory + "\\authenticator-backup\\databases;Version=3;"))
					{
						connection.Open();

						var cmd = connection.CreateCommand();
						cmd.CommandText = "SELECT email,secret,issuer FROM accounts";
						var reader = cmd.ExecuteReader();

						foreach(System.Data.Common.DbDataRecord item in reader)
						{
							items.Add(new GAItem() { email = item.GetValue(0).ToString(), secret = item.GetValue(1).ToString(), issuer = item.GetValue(2).ToString() });
						}

						var qrWriter = new BarcodeWriter();
						qrWriter.Format = BarcodeFormat.QR_CODE;
						qrWriter.Options.Hints.Add(EncodeHintType.CHARACTER_SET, "UTF-8");
						qrWriter.Options.Hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.H);
						qrWriter.Options.Hints.Add(EncodeHintType.MARGIN, 1);
						qrWriter.Options.Hints[EncodeHintType.WIDTH] = 250;
						qrWriter.Options.Hints[EncodeHintType.HEIGHT] = 250;
						
						foreach(var item in items)
						{
							if (string.IsNullOrEmpty(item.issuer))
							{
								if (item.email.Split(' ').Length > 0)
								{
									item.issuer = item.email.Split(' ')[0];
								}
								else
								{
									item.issuer = item.email;
								}

								if (item.issuer.Split(':').Length > 0)
								{
									item.issuer = item.issuer.Split(':')[0];
								}
							}

							var url = string.Format("otpauth://totp/{0}?secret={1}&issuer={2}", item.email, item.secret, item.issuer);
							item.url = url;
							
							var bmp = qrWriter.Write(item.url);
							item.image = BitmapToImageSource(bmp);
						}
					}
					
				}
			}

			lstResults.ItemsSource = items;
		}
		
		private BitmapImage BitmapToImageSource(System.Drawing.Bitmap bitmap)
		{
			using (MemoryStream memory = new MemoryStream())
			{
				bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
				memory.Position = 0;
				BitmapImage bitmapimage = new BitmapImage();
				bitmapimage.BeginInit();
				bitmapimage.StreamSource = memory;
				bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
				bitmapimage.EndInit();
				return bitmapimage;
			}
		}
	}
}
