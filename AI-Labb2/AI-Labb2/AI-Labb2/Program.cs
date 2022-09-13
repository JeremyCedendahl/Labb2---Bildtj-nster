using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace AI_Labb2
{
     class Program
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            int dwDesiredAccess,
            int dwShareMode,
            IntPtr lpSecurityAttributes,
            int dwCreationDisposition,
            int dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetCurrentConsoleFont(
            IntPtr hConsoleOutput,
            bool bMaximumWindow,
            [Out][MarshalAs(UnmanagedType.LPStruct)] ConsoleFontInfo lpConsoleCurrentFont);

        [StructLayout(LayoutKind.Sequential)]
        internal class ConsoleFontInfo
        {
            internal int nFont;
            internal Coord dwFontSize;
        }

        [StructLayout(LayoutKind.Explicit)]
        internal struct Coord
        {
            [FieldOffset(0)]
            internal short X;
            [FieldOffset(2)]
            internal short Y;
        }

        private const int GENERIC_READ = unchecked((int)0x80000000);
        private const int GENERIC_WRITE = 0x40000000;
        private const int FILE_SHARE_READ = 1;
        private const int FILE_SHARE_WRITE = 2;
        private const int INVALID_HANDLE_VALUE = -1;
        private const int OPEN_EXISTING = 3;

        private static ComputerVisionClient cvClient;

        static async Task Main(string[] args)
        {


            bool isAnalyzing = true;

            while (isAnalyzing)
            {
                Console.WriteLine("Press 1 to search in a folder. Press 2 to analyze in project. Press 3 to exit");
                string path = "Image";

                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.WriteLine("Suggested path:" + @"C:\Users\Jermz0r\Desktop\Images\");
                    string directory = Console.ReadLine();
                    if(directory == "")
                    {
                        directory = @"C:\Users\Jermz0r\Desktop\Images\";
                    }
                    path = Path.GetDirectoryName(@directory);
                }
                else if (choice == "2")
                {
                    path = "Image";
                }
                else if (choice == "3")
                {
                    Environment.Exit(0);
                }
                else
                {
                    Console.WriteLine("Invalid choice.");
                    isAnalyzing = false;
          
                }

                if (isAnalyzing)
                {
                    Console.WriteLine("Write the name for the image");


                    string newFileName = Console.ReadLine();
                    Console.WriteLine("Write the type for the image to analyze it");

                    string fileType = Console.ReadLine();
                    string newPath = $"{path}\\{newFileName}{fileType}";

                    try
                    {
                        // Config
                        IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile("appsettings.json");
                        IConfigurationRoot configuration = builder.Build();
                        string cogSvcEndpoint = configuration["Endpoint"];
                        string cogSvcKey = configuration["Key"];

                        if (args.Length > 0)
                        {
                            newFileName = args[0];
                        }


                        // Computer Vision client
                        ApiKeyServiceClientCredentials credentials = new
                        ApiKeyServiceClientCredentials(cogSvcKey);
                        cvClient = new ComputerVisionClient(credentials)
                        {
                            Endpoint = cogSvcEndpoint
                        };



                        // Analyze image
                        await AnalyzeImage(newPath);

                        // Get thumbnail
                        await GetThumbnail(newPath);


                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
                isAnalyzing = true;
            }

           
     
        }

        static async Task AnalyzeImage(string imageFile)
        {
            Console.WriteLine($"Analyzing: {imageFile}");

            // Specify features to be retrieved
            List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
          {
           VisualFeatureTypes.Description,
           VisualFeatureTypes.Tags,
           VisualFeatureTypes.Categories,
           VisualFeatureTypes.Brands,
           VisualFeatureTypes.Objects,
           VisualFeatureTypes.Adult
          };


            using (var imageData = File.OpenRead(imageFile))
            {
                var analysis = await cvClient.AnalyzeImageInStreamAsync(imageData, features);
                foreach (var caption in analysis.Description.Captions)
                {
                    Console.WriteLine($"Description: {caption.Text} (confidence:{caption.Confidence.ToString("P")})");
                }

                if (analysis.Tags.Count > 0)
                {
                    Console.WriteLine("Tags:");
                    foreach (var tag in analysis.Tags)
                    {
                        Console.WriteLine($" -{tag.Name} (confidence:{tag.Confidence.ToString("P")})");
                    }
                }

                List<LandmarksModel> landmarks = new List<LandmarksModel> { };
                List<CelebritiesModel> celebrities = new List<CelebritiesModel> { };
                Console.WriteLine("Categories:");
                foreach (var category in analysis.Categories)
                {
                  
                    Console.WriteLine($" -{category.Name} (confidence:{category.Score.ToString("P")})");
                    if (category.Detail?.Landmarks != null)
                    {
                        foreach (LandmarksModel landmark in category.Detail.Landmarks)
                        {
                            if (!landmarks.Any(item => item.Name == landmark.Name))
                            {
                                landmarks.Add(landmark);
                            }
                        }
                    }
                    if (category.Detail?.Celebrities != null)
                    {
                        foreach (CelebritiesModel celebrity in category.Detail.Celebrities)
                        {
                            if (!celebrities.Any(item => item.Name == celebrity.Name))
                            {
                                celebrities.Add(celebrity);
                            }
                        }
                    }
                }
                if (landmarks.Count > 0)
                {
                    Console.WriteLine("Landmarks:");
                    foreach (LandmarksModel landmark in landmarks)
                    {
                        Console.WriteLine($" -{landmark.Name} (confidence:{landmark.Confidence.ToString("P")})");
                    }
                }
                if (celebrities.Count > 0)
                {
                    Console.WriteLine("Celebrities:");
                    foreach (CelebritiesModel celebrity in celebrities)
                    {
                        Console.WriteLine($" -{celebrity.Name} (confidence:{celebrity.Confidence.ToString("P")})");
                    }
                }
                if (analysis.Brands.Count > 0)
                {
                    Console.WriteLine("Brands:");
                    foreach (var brand in analysis.Brands)
                    {
                        Console.WriteLine($" -{brand.Name} (confidence:{brand.Confidence.ToString("P")})");
                    }
                }
                if (analysis.Objects.Count > 0)
                {
                    Image image = Image.FromFile(imageFile);
                    Graphics graphics = Graphics.FromImage(image);
                    Pen pen = new Pen(Color.Red, 3);
                    Font font = new Font("Arial", 22);
                    SolidBrush brush = new SolidBrush(Color.LightCoral);
                    foreach (var detectedObject in analysis.Objects)
                    {
                       
                        Console.WriteLine($" -{detectedObject.ObjectProperty} (confidence:{detectedObject.Confidence.ToString("P")})");
                        var r = detectedObject.Rectangle;
                        Rectangle rect = new Rectangle(r.X, r.Y, r.W, r.H);
                        graphics.DrawRectangle(pen, rect);
                        graphics.DrawString(detectedObject.ObjectProperty, font, brush, r.X, r.Y);
                    }
                    
                    String output_file = "boundingBoxedImage.jpg";
                    image.Save(output_file);
                    Console.WriteLine(" Results saved in " + output_file);
                }
              
                string ratings = $"Ratings:\n -Adult: {analysis.Adult.IsAdultContent}\n -Racy:{analysis.Adult.IsRacyContent}\n -Gore: {analysis.Adult.IsGoryContent} \n";
                Console.WriteLine(ratings);

                Console.WriteLine("\nPress any key to show image");
                Console.ReadKey();
                ShowImage("boundingBoxedImage.jpg", 60);

            }

        }

        static async Task GetThumbnail(string imageFile)
        {
           
            Console.WriteLine("\nGenerating thumbnail...");
            using (var imageData = File.OpenRead(imageFile))
            {

                Console.WriteLine("Desired thumbnail size?");
                string sizeSelection = Console.ReadLine();

                var size = int.Parse(sizeSelection);
                var thumbnailStream = await cvClient.GenerateThumbnailInStreamAsync(size,
               size, imageData, true);
                string thumbnailFileName = "thumbnail.jpg";
                using (Stream thumbnailFile = File.Create(thumbnailFileName))
                {
                    thumbnailStream.CopyTo(thumbnailFile);
                }
                Console.WriteLine($"Thumbnail saved in {thumbnailFileName}\n");
                Console.WriteLine("\nPress any key to show thumbnail.");
                Console.ReadKey();
                ShowImage("thumbnail.jpg", 20);
                Console.WriteLine("\nPress any key to continue.");
                Console.ReadKey();
                
            }
            
        }
        private static void ShowImage(string path, int size)
        {
            Console.Clear();
            Point location = new Point(10, 1);
            Size imageSize = new Size(size, size / 2); 

         
            Console.SetCursorPosition(location.X - 1, location.Y);
            Console.Write(">");
            Console.SetCursorPosition(location.X + imageSize.Width, location.Y);
            Console.Write("<");
            Console.SetCursorPosition(location.X - 1, location.Y + imageSize.Height - 1);
            Console.Write(">");
            Console.SetCursorPosition(location.X + imageSize.Width, location.Y + imageSize.Height - 1);
            Console.Write("<");

            using (Graphics g = Graphics.FromHwnd(GetConsoleWindow()))
            {
                using (Image image = Image.FromFile(path))
                {
                    Size fontSize = GetConsoleFontSize();

      
                    Rectangle imageRect = new Rectangle(
                        location.X * fontSize.Width,
                        location.Y * fontSize.Height,
                        imageSize.Width * fontSize.Width,
                        imageSize.Height * fontSize.Height);
                    g.DrawImage(image, imageRect);
                }
            }
        }

        private static Size GetConsoleFontSize()
        {
          
            IntPtr outHandle = CreateFile("CONOUT$", GENERIC_READ | GENERIC_WRITE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);
            int errorCode = Marshal.GetLastWin32Error();
            if (outHandle.ToInt32() == INVALID_HANDLE_VALUE)
            {
                throw new IOException("Unable to open CONOUT$", errorCode);
            }

            ConsoleFontInfo cfi = new ConsoleFontInfo();
            if (!GetCurrentConsoleFont(outHandle, false, cfi))
            {
                throw new InvalidOperationException("Unable to get font information.");
            }

            return new Size(cfi.dwFontSize.X, cfi.dwFontSize.Y);
        }
    }
}



    
