using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Renci.SshNet;
using Path = System.IO.Path;

namespace Progowanie
{
    /// <summary>
    /// Interaction logic for rPi.xaml
    /// </summary>
    public partial class rPi : Window
    {
        private string sciezka;
        private double engrTime = 0.2;
        private double idleTime = 0.005;
        public rPi()
        {
            InitializeComponent();
            sciezka = MainWindow.GcPath;
            gcPath.Content = sciezka;
            expTime.Content = Math.Round(PredictTime(sciezka)) + " sekund";
            wymiaryLb.Content = MainWindow.width + "x" + MainWindow.height;
        }
        /// <summary>
        /// Funkcja służy do obliczania szacowanego czasu grawerowania.
        /// </summary>
        /// <param name="path">Ścieżka do pliku z instrukcjami</param>
        /// <returns>Wyliczony czas w sekundach</returns>
        private double PredictTime(string path)
        {
            var lines = File.ReadAllLines(path);
            double etime = 0;
            double itime = 0;
            foreach (var line in lines)
            {
                foreach (var op in line)
                {
                    if (op == '1' || op == '3')
                        etime += 4 * engrTime;
                    else
                        itime += 4 * idleTime;
                }
            }
            etime += (etime / 90) * 30;
            return (etime + itime) * 1.222;
        }
        /// <summary>
        /// Obsługa przycisku "Start".
        /// </summary>
        private void startBtn_Click(object sender, RoutedEventArgs e)
        {
            scrl.Visibility = Visibility.Visible;
            //wysyłanie pliku z instrukcjami do Raspberry Pi
            using (var scp = new ScpClient(MainWindow.RasPiAddress, "zwo", "123456"))
            {
                scp.Connect();
                scp.Upload(new FileInfo(sciezka), Path.GetFileName(sciezka));
                scp.Disconnect();
            }
            //Uruchomienie sterownika
            SshCommand x;
            using (var client = new SshClient(MainWindow.RasPiAddress, "zwo", "123456"))
            {
                client.Connect();
                //sprawdzenie, czy sterownik wcześniej nie pracował 
                //(w przypadku np. trwającego jeszcze procesu grawerowania poprzedniego pliku)
                x = client.RunCommand("ps -aux |grep drv.py");
                var m = x.Result.Split('\n').FirstOrDefault(r => r.Contains("sudo"));
                if (m == null)
                {
                    MessageBox.Show("Uruchomiono sterownik. Nie patrz na laser!!!");
                    var gc = Path.GetFileName(sciezka);

                    client.RunCommand("sudo ./drv.py " + gc);
                }
                client.Disconnect();

            }
        }
    }

}