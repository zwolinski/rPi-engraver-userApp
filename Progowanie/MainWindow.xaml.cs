using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Renci.SshNet;
using Path = System.IO.Path;

namespace Progowanie
{
    /// <summary>
    /// Główne okno aplikacji
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Deklaracje zmiennych
        private BitmapImage bitmapImage;
        private WriteableBitmap bitmap;
        private int stride;
        private int size;
        public static int width { get; set; }
        public static int height { get; set; }
        private byte[] pixels;
        private int[] histogram;
        private bool loaded = false;
        private bool processed = false;
        private string fileName;
        public static string GcPath { get; set; }
        public static string RasPiAddress { get; set; }
        public static string Sciezka { get; set; }
        private BitmapImage ok = new BitmapImage(new Uri(@"/../../img/ok.png", UriKind.Relative));
        private BitmapImage error = new BitmapImage(new Uri(@"/../../img/error.png", UriKind.Relative));
        private BitmapImage arrowBl = new BitmapImage(new Uri(@"/../../img/arrow_blue.png", UriKind.Relative));
        private BitmapImage arrowGrey = new BitmapImage(new Uri(@"/../../img/arrow_grey.png", UriKind.Relative));
        private BitmapImage arrowGr = new BitmapImage(new Uri(@"/../../img/arrow_green.png", UriKind.Relative));
        private BitmapImage arrowAnim = new BitmapImage(new Uri(@"/../../img/arrow_anim.png", UriKind.Relative));
        #endregion
        public MainWindow()
        {
            InitializeComponent();
            //tworzenie i zerowanie tablicy przechowującej histogram obrazu
            histogram = new int[256];

            for (int i = 0; i < 256; ++i)
                histogram[i] = 0;

            RasPiAddress = "192.168.42.1";
        }

        /// <summary>
        /// Metoda wczytuje plik graficzny z pliku.
        /// </summary>
        /// <param name="sciezka">Ścieżka do wczytywanego pliku</param>
        private void LadujObraz(string sciezka)
        {
            bitmapImage =
                new BitmapImage(
                    new Uri(sciezka));
            image.Source = bitmapImage;

            width = bitmapImage.PixelWidth;
            height = bitmapImage.PixelHeight;

            stride = width * 4;
            size = height * stride;
            //ustawienie zmiennych pomocniczych
            loaded = true;
            image6.Source = ok;
            label4.FontWeight = FontWeights.Normal;
            label5.FontWeight = FontWeights.Bold;
            image7.Source = arrowBl;
        }

        /// <summary>
        /// Metoda odpowiada za obsługę przycisku "Skala szarości".
        /// Przekształca obraz do skali szarości, a także tworzy jego histogram.
        /// 
        /// </summary>
        private void grey_Click(object sender, RoutedEventArgs e)
        {
            if (!loaded) return;
            bitmap = new WriteableBitmap(bitmapImage);

            pixels = new byte[size];
            bitmapImage.CopyPixels(pixels, stride, 0);

            double color = 0;
            int index = 0;
            for (int i = 0; i < pixels.Length / 4; ++i)
            {
                //skala szarości
                color = (double)(pixels[index] + pixels[index + 1] + pixels[index + 2]) / 3;
                pixels[index] = (byte)color;
                pixels[index + 1] = (byte)color;
                pixels[index + 2] = (byte)color;
                pixels[index + 3] = 255;

                //histogram
                histogram[(int)Math.Round(color)] += 1;
                index += 4;

                processed = true;
            }
            //zapisywanie histogramu do pliku
            string[] result = histogram.Select(x => x.ToString()).ToArray();
            System.IO.File.WriteAllLines("hist.txt", result);
            
            //wyświetlanie przetworzonego obrazu
            Int32Rect rect = new Int32Rect(0, 0, width, height);
            bitmap.WritePixels(rect, pixels, stride, 0);
            image.Source = bitmap;

            //ustawienie dodatkowych pól
            image4.Source = ok;
            label5.FontWeight = FontWeights.Normal;
            label6.FontWeight = FontWeights.Bold;
            image7.Source = arrowGr;
            image8.Source = arrowBl;
        }

        /// <summary>
        /// Obsługa przycisku "Binaryzacja".
        /// </summary>
        private void bin_Click(object sender, RoutedEventArgs e)
        {
            if (!processed) return;
            Progowanie();
        }

        /// <summary>
        /// Funkcja wyznacza próg binaryzacji metodą Otsu (na podstawie histogramu).
        /// </summary>
        /// <param name="hist">Tablica z histogramem przetwarzanego obrazu</param>
        /// <param name="total">Całkowita ilość pikseli w obrazie</param>
        /// <returns>Wyznaczony próg binaryzacji</returns>
        private double Otsu(int[] hist, int total)
        {
            var sum = 0;
            for (var i = 1; i < hist.Length; ++i)
                sum += i * histogram[i];
            double sumB = 0;
            double wB = 0;
            double wF = 0;
            double mB;
            double mF;
            double max = 0.0;
            double between = 0.0;
            double threshold1 = 0.0;

            for (var i = 0; i < hist.Length; ++i)
            {
                wB += hist[i];
                if (wB == 0)
                    continue;
                wF = total - wB;
                if (wF == 0)
                    break;
                sumB += i * hist[i];
                mB = sumB / wB;
                mF = (sum - sumB) / wF;
                between = wB * wF * (mB - mF) * (mB - mF);
                if (between > max)
                {
                    threshold1 = i;
                    max = between;
                }
            }
            return threshold1;
        }

        /// <summary>
        /// Metoda odpowiada za binaryzację obrazu.
        /// </summary>
        private void Progowanie()
        {
            var index = 0;

            bitmap.CopyPixels(pixels, stride, 0);
            Color czarny = Colors.Black;
            Color bialy = Colors.White;
            int prog = 180;

            prog = (int)Math.Round(Otsu(histogram, pixels.Length / 4));
            label.Content = prog;

            for (int i = 0; i < pixels.Length / 4; ++i)
            {
                if (pixels[index + 2] > prog && pixels[index + 1] > prog && pixels[index + 0] > prog)
                {
                    pixels[index] = bialy.R;
                    pixels[index + 1] = bialy.G;
                    pixels[index + 2] = bialy.B;
                    pixels[index + 3] = 255;

                    index += 4;
                }
                else
                {
                    pixels[index] = czarny.R;
                    pixels[index + 1] = czarny.G;
                    pixels[index + 2] = czarny.B;
                    pixels[index + 3] = 255;

                    index += 4;
                }
            }
            // zmiana rozmiaru obrazu
            var rect = new Int32Rect(0, 0, width, height);
            bitmap.WritePixels(rect, pixels, stride, 0);
            image.Source = bitmap;
            image5.Source = ok;
            label6.FontWeight = FontWeights.Normal;
            label7.FontWeight = FontWeights.Bold;
            image8.Source = arrowGr;
            image9.Source = arrowBl;
        }

        /// <summary>
        /// Obsługa przycisku "Otwórz obraz". Otwiera plik graficzny na podstawie 
        /// wyboru użytkownika.
        /// </summary>
        private void open_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Multiselect = false,
                AddExtension = true
            };
            ofd.ShowDialog();
            Sciezka = ofd.FileName;
            fileName = ofd.SafeFileName;
            if (Sciezka != "")
                LadujObraz(Sciezka);
        }
        /// <summary>
        /// Obsługa przycisku "Zapisz obraz". Zapisuje plik graficzny do pliku, 
        /// umożliwiając wskazanie miejsca do zapisania.
        /// </summary>
        private void save_Click(object sender, RoutedEventArgs e)
        {
            if (!processed) return;
            SaveFileDialog sfd = new SaveFileDialog
            {
                OverwritePrompt = true,
                FileName = Path.GetFileNameWithoutExtension(fileName) + "BIN" + Path.GetExtension(fileName),
                Filter = "JPG (*.jpg)|*.jpg|PNG (*.png)|*.png|GIF (*.gif)|*.gif"
            };


            if (sfd.ShowDialog() == true)
                using (var fileStream = new FileStream(sfd.FileName, FileMode.Create))
                {
                    BitmapEncoder encoder;
                    var extension = Path.GetExtension(sfd.FileName);
                    if (extension == null) return;
                    switch (extension.ToLower())
                    {
                        case ".jpg":
                            encoder = new JpegBitmapEncoder();
                            break;
                        case ".png":
                            encoder = new PngBitmapEncoder();
                            break;
                        case ".gif":
                            encoder = new GifBitmapEncoder();
                            break;
                        default:
                            return;
                    }
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fileStream);
                }
        }
        /// <summary>
        /// Generowanie kodu sterującego urządzeniem
        /// Oznaczenia kodów cyfrowych
        /// 0 - ruch w prawo bez grawerowania
        /// 1 - ruch w prawo z grawerowaniem
        /// 2 - ruch w lewo bez grawerowania
        /// 3 - ruch w lewo z grawerowaniem
        /// 4 - ruch w dół bez grawerowania
        /// 5 - ruch w górę bez grawerowania
        /// END - koniec pracy, powrót do punktu początkowego (0, 0)
        /// 
        /// Struktura pliku (przykład dla obrazu 6x3 px):
        /// new
        /// x x x x x x
        /// x x x x x x
        /// x x x x x x
        /// END
        /// </summary>
        private void GenerujGcode()
        {
            var w = bitmap.Width;
            var h = bitmap.Height;

            string line = "";
            GcPath = Sciezka + ".txt";
            bool odd = true;
            if (File.Exists(GcPath))
            {
                File.Delete(GcPath);
            }
            File.AppendAllText(GcPath, "new\n");

            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w; j++)
                {
                    //czarny
                    if (GetPixel(bitmap, j, i) == Colors.Black)
                    {
                        if (odd)
                            line += "1 ";
                        else
                            line += " 3";
                    }
                    //biały
                    else
                    {
                        if (odd)
                            line += "0 ";
                        else
                            line += " 2";
                    }
                }
                if (!odd)
                {
                    string output = new string(line.ToCharArray().Reverse().ToArray());
                    line = output;
                }
                line += "4";
                line += "\n";
                File.AppendAllText(GcPath, line);
                line = "";
                odd = !odd;
            }
            File.AppendAllText(GcPath, "END");
            label7.FontWeight = FontWeights.Normal;
            label8.FontWeight = FontWeights.Bold;
            image9.Source = arrowGr;
            image10.Source = arrowBl;
            image11.Source = ok;
        }

        /// <summary>
        /// Metoda pobiera kolor danego piksela.
        /// </summary>
        /// <param name="img">Badany obraz</param>
        /// <param name="x">Współrzędna pozioma badanego piksela</param>
        /// <param name="y">Współrzędna pionowa badanego piksela</param>
        /// <returns></returns>
        private Color GetPixel(WriteableBitmap img, int x, int y)
        {
            if (pixels == null)
            {
                pixels = new byte[size];
                img.CopyPixels(pixels, stride, 0);
            }
            int index = y * stride + 4 * x;

            byte r = pixels[index];
            byte g = pixels[index + 1];
            byte b = pixels[index + 2];
            byte a = pixels[index + 3];

            return new Color { A = a, B = b, G = g, R = r };
        }

        /// <summary>
        /// Obsługa przycisku "Generuj instrukcje".
        /// </summary>
        private void gcode_Click(object sender, RoutedEventArgs e)
        {
            GenerujGcode();
        }

        /// <summary>
        /// Obsługa przycisku "Połącz z rPi".
        /// </summary>
        private void rPi_Click(object sender, RoutedEventArgs e)
        {
            using (var client = new SshClient(RasPiAddress, "zwo", "123456"))
            {
                try
                {
                    client.Connect();

                    rPiUst.Visibility = Visibility.Visible;
                    var x = client.RunCommand("ps -aux |grep drv.py");

                    var m = x.Result.Split('\n').FirstOrDefault(r => r.Contains("sudo"));
                    if (m == null && x.Error != "")
                    {
                       image2.Source = error;
                    }
                    else
                    {
                        image2.Source = ok;

                        if (m != null && m.Length > 3)
                        {
                            image3.Source = error;
                        }
                        else
                        {
                            image3.Source = ok;
                        }
                    }
                    client.Disconnect();
                }
                catch (SocketException ex)
                {
                    image2.Source = error;
                    image3.Source = error;
                    MessageBox.Show(ex.Message + "\nProszę wprowadzić adres Raspberry Pi.");
                    rPiTb.Visibility = Visibility.Visible;
                    label3.Visibility = Visibility.Visible;
                    rPiAddOk.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Inny błąd. " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Obsługa przycisku "Podsumowanie". Otwiera nowe okno.
        /// </summary>
        private void rPiUst_Click(object sender, RoutedEventArgs e)
        {
            rPi rPi = new rPi();
            rPi.Show();
        }

        /// <summary>
        /// Odczytanie i ustawienie wprowadzonego przez użytkownika adresu Raspberry Pi.
        /// </summary>
        private void rPiAddOk_Click(object sender, RoutedEventArgs e)
        {
            RasPiAddress = rPiTb.Text;
        }
    }

}
