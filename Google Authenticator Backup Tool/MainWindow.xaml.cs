using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Gma.QrCodeNet.Encoding;
using System.Data.SQLite;
using System.Data;

namespace Google_Authenticator_Backup_Tool
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public class GAItem
		{
			public string email { set; get; }
			public string issuer { set; get; }
			public string secret { set; get; }
			public string url { set; get; }
		}

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
					sw.WriteLine("cd %temp%");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("mkdir authenticator-backup");
					System.Threading.Thread.Sleep(500);
					sw.WriteLine("cd \"%temp%\\authenticator-backup\"");

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

					using (SQLiteConnection connection = new SQLiteConnection("Data Source="+Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)+"\\temp\\authenticator-backup\\databases;Version=3;"))
					{
						connection.Open();

						var cmd = connection.CreateCommand();
						cmd.CommandText = "SELECT email,secret,issuer FROM accounts";
						var reader = cmd.ExecuteReader();

						foreach(System.Data.Common.DbDataRecord item in reader)
						{
							items.Add(new GAItem() { email = item.GetValue(0).ToString(), secret = item.GetValue(1).ToString(), issuer = item.GetValue(2).ToString() });
						}

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
						}
					}
					
				}
			}

			lstResults.ItemsSource = items;

			/*
			mkdir authenticator-backup
			cd authenticator-backup
			adb root
			adb pull /data/data/com.google.android.apps.authenticator2/databases/databases
			sqlite3 ./databases "select * from accounts"


			import qrcode
			import sqlite3
			conn = sqlite3.connect('./databases')
			c = conn.cursor()

			for idx, (email, secret, issuer) in enumerate(c.execute("SELECT email,secret,issuer FROM accounts").fetchall()):
			   if issuer==None:
				   if len(email.split(" "))>0:
					   issuer=email.split(" ")[0]
				   else:
					   issuer=email

				   if len(issuer.split(":"))>0:
					   issuer=issuer.split(":")[0]

				   print("If the following issuer looks wrong, enter a new value. If it's OK, just press ENTER.")
				   newIssuer=input(issuer)
				   if len(newIssuer)>0:
					   issuer=newIssuer
			   url = 'otpauth://totp/{}?secret={}&issuer={}'.format(email, secret, issuer)
			   print (url)
			   im = qrcode.make(url)
			   im.save('./{}.png'.format(idx))
			*/
		}
	}
}
