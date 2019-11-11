using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Net;
using XZ.NET;
using PdfSharp.Pdf;
using PdfSharp.Drawing;
using ImageResizer;
using System.Configuration;

namespace MTGText2Pdf
{
    class Program
    {
        static readonly string cache_dir = ".\\.cache\\";
        static readonly string cache_image_dir = cache_dir + "image\\";
        static readonly string cache_data_dir = cache_dir + "data\\";
        static readonly string cache_data_file = cache_data_dir + "data.bin";
        static readonly string cache_data_printings_file = cache_data_dir + "AllPrintings.json";
        static readonly string cache_data_printings_archive = cache_data_dir + "AllPrintings.json.xz";
        static readonly string cache_data_tokens_file = cache_data_dir + "tokens.xml";
        static readonly string allsets_url = "https://mtgjson.com/json/AllSets.json.xz";
        static readonly string tokens_url = "https://raw.githubusercontent.com/Cockatrice/Magic-Token/master/tokens.xml";

        static int step;
        static int max_steps;
        static DateTime download_start;
        static bool proceed;
        static CardsManager manager;
        static List<Deck> decks;
        static Deck all_cards;
        static Deck imaged_cards;

        static void Main(string[] args)
        {
            step = 0;
            max_steps = 1;

            if (!Directory.Exists(cache_dir))
            {
                Directory.CreateDirectory(cache_dir);
            }

            if (!Directory.Exists(cache_image_dir))
            {
                Directory.CreateDirectory(cache_image_dir);
            }

            if (!Directory.Exists(cache_data_dir))
            {
                Directory.CreateDirectory(cache_data_dir);
            }

            max_steps += args.Length;

            max_steps += 4;

            InitializeCardsManager();

            decks = new List<Deck>();
            all_cards = new Deck();
            imaged_cards = new Deck();

            foreach (string deck_filename in args)
            {
                Console.Write(GetSteps() + "Loading deck \"" + deck_filename + "\" ...");

                Deck new_deck = Deck.Parse(manager, deck_filename);
                decks.Add(new_deck);
                all_cards.Add(new_deck);

                Console.WriteLine(" Done");
            }

            Console.WriteLine(GetSteps() + "Caching " + all_cards.cards.Count.ToString() + " unique images...");

            InitializeImages();

            Console.WriteLine(GetSteps() + "Image caching complete");

            Console.WriteLine(GetSteps() + "Generating PDF");

            GeneratePDF();

            Console.WriteLine(GetSteps() + "PDF saved");

            Console.ReadKey();
        }

        private static void Download(string url, string file, bool async = false)
        {
            using (var client = new WebClient())
            {
                if (async)
                {
                    client.DownloadProgressChanged += Client_DownloadProgressChanged;
                    client.DownloadFileCompleted += Client_DownloadFileCompleted;
                    download_start = DateTime.Now;
                    client.DownloadFileAsync(new Uri(url), file);
                    proceed = false;
                    while (!proceed)
                    {
                        Thread.Sleep(16);
                    }
                }
                else
                {
                    client.DownloadFile(new Uri(url), file);
                }
            }
        }

        private static void InitializeCardsManager()
        {
            manager = new CardsManager();

            if (!File.Exists(cache_data_file))
            {
                max_steps += 2;
                if (!File.Exists(cache_data_printings_file))
                {
                    max_steps += 3;
                    if (!File.Exists(cache_data_printings_archive))
                    {
                        max_steps += 2;
                    }

                    if (!File.Exists(cache_data_tokens_file))
                    {
                        max_steps += 2;
                    }
                }
            }

            if (!File.Exists(cache_data_file))
            {
                Console.WriteLine(GetSteps() + "Cache (" + cache_data_file + ") not found, creating cache...");
                if (!File.Exists(cache_data_printings_file))
                {
                    Console.WriteLine(GetSteps() + "Card data (" + cache_data_printings_file + ") not found...");
                    if (!File.Exists(cache_data_printings_archive))
                    {
                        Console.WriteLine(GetSteps() + "Card archive (" + cache_data_printings_archive + ") not found, downloading...");

                        Download(allsets_url, cache_data_printings_archive, true);

                        Console.WriteLine(GetSteps() + "Download complete");
                    }

                    if (!File.Exists(cache_data_tokens_file))
                    {
                        Console.WriteLine(GetSteps() + "Token XML (" + cache_data_tokens_file + ") not found, downloading...");

                        Download(tokens_url, cache_data_tokens_file);

                        Console.WriteLine(GetSteps() + "Download complete");
                    }

                    Console.WriteLine(GetSteps() + "Extracting card archive...");
                    using (Stream xz = new XZInputStream(File.OpenRead(cache_data_printings_archive)))
                    using (Stream stream = new FileStream(cache_data_printings_file, FileMode.OpenOrCreate))
                    {
                        xz.CopyTo(stream);
                    }
                    Console.WriteLine(GetSteps() + "Extraction complete");
                }

                Console.WriteLine(GetSteps() + "Processing data set (cards and tokens)...");

                string string_data = File.ReadAllText(cache_data_printings_file);
                JObject data = JObject.Parse(string_data);

                List<Thread> threads = new List<Thread>();
                foreach (var set in data)
                {
                    threads.Add(new Thread(() =>
                    {
                        Set.Parse(set.Key, set.Value.ToObject<JObject>(), manager);
                    }));
                }


                foreach (var thread in threads)
                {
                    thread.Start();
                }

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                WriteToBinaryFile(cache_data_file, manager);

                Console.WriteLine(GetSteps() + "Cache (" + cache_data_file + ") creation completed.");
            }
            else
            {
                Console.Write(GetSteps() + "Cache (" + cache_data_file + ") found! Loading...");

                manager = ReadFromBinaryFile<CardsManager>(cache_data_file);

                Console.WriteLine(" Done");
            }
        }

        private static string GetCardImage(ImageAble card)
        {
            if (card == null)
            {
                return null;
            }

            if(File.Exists(cache_image_dir + card.GetImageFileName()))
            {
                return cache_image_dir + card.GetImageFileName();
            }

            List<ImageAble> banned = new List<ImageAble>();

            while (true)
            {
                foreach (string url in card.GetImageUrls())
                {
                    if (File.Exists(cache_image_dir + card.GetImageFileName()))
                    {
                        return cache_image_dir + card.GetImageFileName();
                    }
                }

                banned.Add(card);
                card = manager.ReNew(card, banned);

                if (card == null)
                {
                    break;
                }
            }

            return null;
        }

        private static void ProcessCard(ImageAble card, int amount)
        {
            if(card == null)
            {
                return;
            }

            if(File.Exists(cache_image_dir + card.GetImageFileName()))
            {
                imaged_cards.Add(card, amount);
                card.SetCachedImage(cache_image_dir + card.GetImageFileName());
                return;
            }

            List<ImageAble> banned = new List<ImageAble>();
            bool downloaded = false;
            while (!downloaded)
            {
                foreach (string url in card.GetImageUrls())
                {
                    try
                    {
                        Thread.Sleep(100);
                        Download(url, cache_image_dir + card.GetImageFileName());
                        imaged_cards.Add(card, amount);
                        card.SetCachedImage(cache_image_dir + card.GetImageFileName());
                        downloaded = true;
                        break;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }

                if(!downloaded)
                {
                    banned.Add(card);
                    card = manager.ReNew(card, banned);
                }

                if(card == null)
                {
                    Console.WriteLine("[E] Error: Failed to download card \"" + card.GetName() + "\"");
                    break;
                }
            }
        }

        private static void InitializeImages()
        {
            int i = 0;
            int max = all_cards.cards.Count;

            foreach (var card in all_cards.cards)
            {
                ClearCurrentConsoleLine();
                Console.Write("\tProgress: [" + i.ToString() + "/" + max.ToString() + "]");

                ProcessCard(card.Key, card.Value);

                ++i;
                ClearCurrentConsoleLine();
                Console.Write("\tProgress: [" + i.ToString() + "/" + max.ToString() + "]");
            }
            Console.WriteLine(" ");
        }

        private static void GeneratePDF()
        {
            PdfDocument document = new PdfDocument();
            document.Options.FlateEncodeMode = PdfFlateEncodeMode.BestCompression;
            document.Options.UseFlateDecoderForJpegImages = PdfUseFlateDecoderForJpegImages.Automatic;
            document.Options.NoCompression = false;

            document.Options.CompressContentStreams = true;

            int current_images = 0;
            int progress = 0;
            int max = imaged_cards.cards.Count;

            PdfPage page = document.AddPage();
            XGraphics gfx = XGraphics.FromPdfPage(page);

            bool enable_compression = bool.Parse(ConfigurationManager.AppSettings["enableCompression"]);

            ResizeSettings settings = new ResizeSettings();
            settings.Format = "jpg";

            int maxHeight = int.Parse(ConfigurationManager.AppSettings["maxHeight"]);
            if (maxHeight > 0)
            {
                settings.MaxHeight = maxHeight;
            }

            int quality = int.Parse(ConfigurationManager.AppSettings["imageQuality"]);
            if (quality > 0 && quality <= 100)
            {
                settings.Quality = quality;
            }

            foreach (var entry in imaged_cards.cards)
            {
                ClearCurrentConsoleLine();
                Console.Write("\tProgress: [" + progress.ToString() + "/" + max.ToString() + "]");


                for (int i = 0; i < entry.Value; ++i)
                {
                    XImage img;

                    if (enable_compression)
                    {
                        MemoryStream stream = new MemoryStream();
                        ImageBuilder.Current.Build(entry.Key.GetCachedImage(), stream, settings);
                        img = XImage.FromStream(stream);
                    }
                    else
                    {
                        img = XImage.FromFile(entry.Key.GetCachedImage());
                    }

                    double a4_width = 210.0;
                    double a4_height = 297.0;

                    double w_factor = gfx.PageSize.Width;
                    double h_factor = gfx.PageSize.Height;

                    // A4 = 210x297 mm , MTG 'CC' format = 54 x 85 mm
                    // 40 mm total, 20 mm side margin horizontal
                    // 27 mm total, 13.5mm side margin vertical

                    double x = 20.0 / a4_width * w_factor;
                    double y = 13.5 / a4_height * h_factor;
                    double w = 85.0 / a4_width * w_factor;
                    double h = 54.0 / a4_height * h_factor;

                    double column = (current_images % 2);
                    double row = 0.26 + (current_images / 2); // I really don't know why 0.26 is the perfect number, trial and error

                    var state = gfx.Save();

                    gfx.RotateAtTransform(-90.0, new XPoint(x, y));
                    gfx.TranslateTransform(-x - h * row, y + w * column);
                    gfx.DrawImage(img, 0, 0, h, w);

                    gfx.Restore(state);

                    if (++current_images % 10 == 0)
                    {
                        current_images = 0;

                        gfx.Dispose();

                        page = document.AddPage();
                        gfx = XGraphics.FromPdfPage(page);
                    }
                }

                ++progress;
                ClearCurrentConsoleLine();
                Console.Write("\tProgress: [" + progress.ToString() + "/" + max.ToString() + "]");

            }
            Console.WriteLine(" ");

           document.Save("cards.pdf");
        }

        static string GetSteps()
        {
            ++step;
            return "[" + step.ToString() + "/" + max_steps.ToString() + "] ";
        }

        private static void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            proceed = true;
        }

        public static void ClearCurrentConsoleLine()
        {
            Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");
        }

        private static string SpeedToString(double speed)
        {
            string[] suf = { " bits/s", " Kbit/s", " Mbit/s", " Gbit/s", " Tbit/s", " Pbit/s", " Ebit/s" };
            if (speed < 1.0)
                return "0" + suf[0];
            long bits = (long)Math.Abs(speed);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bits, 1024)));
            double num = Math.Round(bits / Math.Pow(1024, place), 1);
            return (Math.Sign(speed) * num).ToString() + suf[place];
        }

        private static void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            TimeSpan diff = (DateTime.Now - download_start);
            if (diff.TotalSeconds > 0.0)
            {
                Console.Title = e.ProgressPercentage.ToString() + "% | " + SpeedToString(e.BytesReceived * 8.0 / diff.TotalSeconds);
            }
        }

        /// <summary>
        /// Writes the given object instance to a binary file.
        /// <para>Object type (and all child types) must be decorated with the [Serializable] attribute.</para>
        /// <para>To prevent a variable from being serialized, decorate it with the [NonSerialized] attribute; cannot be applied to properties.</para>
        /// </summary>
        /// <typeparam name="T">The type of object being written to the binary file.</typeparam>
        /// <param name="filePath">The file path to write the object instance to.</param>
        /// <param name="objectToWrite">The object instance to write to the binary file.</param>
        /// <param name="append">If false the file will be overwritten if it already exists. If true the contents will be appended to the file.</param>
        public static void WriteToBinaryFile<T>(string filePath, T objectToWrite, bool append = false)
        {
            using (Stream stream = File.Open(filePath, append ? FileMode.Append : FileMode.Create))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(stream, objectToWrite);
            }
        }

        /// <summary>
        /// Reads an object instance from a binary file.
        /// </summary>
        /// <typeparam name="T">The type of object to read from the binary file.</typeparam>
        /// <param name="filePath">The file path to read the object instance from.</param>
        /// <returns>Returns a new instance of the object read from the binary file.</returns>
        public static T ReadFromBinaryFile<T>(string filePath)
        {
            using (Stream stream = File.Open(filePath, FileMode.Open))
            {
                var binaryFormatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
                return (T)binaryFormatter.Deserialize(stream);
            }
        }
    }
}
