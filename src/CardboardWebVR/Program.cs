// <copyright file="Program.cs" company="Matthew Justice">
//     Copyright (c) Matthew Justice. All rights reserved.
// </copyright>

namespace CardboardWebVR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Newtonsoft.Json;

    /// <summary>
    /// The program
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The assets folder name
        /// </summary>
        private const string AssetsFolderName = "assets";

        /// <summary>
        /// The scripts folder name
        /// </summary>
        private const string ScriptsFolderName = "scripts";

        /// <summary>
        /// The size (width and height) of the preview images in pixels
        /// </summary>
        private const int PreviewImageSize = 1024;

        /// <summary>
        /// The image carousel radius in meters.
        /// </summary>
        private const double CarouselRadius = 2.0;

        /// <summary>
        /// The angle in degrees of reserved area on the front of the image carousel
        /// </summary>
        private const double CarouselAngleReserved = 90.0;

        /// <summary>
        /// The height off the ground, in meters, of the center of the image carousel
        /// </summary>
        private const double CarouselHeight = 1.6;

        /// <summary>
        /// The radius in meters of a carousel navigation orb
        /// </summary>
        private const double CarouselNavigationOrbRadius = 0.1;

        /// <summary>
        /// The fractional space that a carousel image is allowed to use.
        /// Each image has a portion of the carousel circumference it could use.
        /// This number is the percentage (expressed as a fraction) of that
        /// space that should be filled.
        /// </summary>
        private const double CarouselImageSpaceFraction = 0.85;

        /// <summary>
        /// The maximum size (width and height) a carousel image can be in meters
        /// </summary>
        private const double CarouselMaxImageSizeInMeters = 1.0;

        /// <summary>
        /// The resource dictionary includes a list of static resources
        /// that will need to be written to disk. The key is the resource
        /// name and the value is the relative path where it should be written.
        /// </summary>
        private static readonly Dictionary<string, string> ResourceDictionary = new Dictionary<string, string>
        {
            { "CardboardWebVR.web_template.scripts.aframe-stereo-component.min.js", $"{ScriptsFolderName}/aframe-stereo-component.min.js" },
            { "CardboardWebVR.web_template.scripts.cardboard-webvr.js", $"{ScriptsFolderName}/cardboard-webvr.js" },
            { "CardboardWebVR.web_template.assets.start.png", $"{AssetsFolderName}/start.png" },
        };

        /// <summary>
        /// The cardboard photos
        /// </summary>
        private static readonly List<CardboardPhoto> CardboardPhotos = new List<CardboardPhoto>();

        /// <summary>
        /// Defines the entry point of the application.
        /// </summary>
        /// <param name="args">The arguments.</param>
        public static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            // Get the command line arguments
            var inputFolder = args[0];
            var outputFolder = args[1];

            // Create the output directory structure
            Directory.CreateDirectory(outputFolder);
            Directory.CreateDirectory(Path.Combine(outputFolder, ScriptsFolderName));
            var assetsFolder = Path.Combine(outputFolder, AssetsFolderName);
            Directory.CreateDirectory(assetsFolder);

            // Add the initial entry in the our cardboard photo list,
            // This is required later when the list is serialized to JSON
            var startPhoto = new CardboardPhoto
            {
                LeftImageId = "#start",
                RightImageId = "#start",
                Caption = "Welcome"
            };
            CardboardPhotos.Add(startPhoto);

            // Try to process the input as a directory or file.
            if (Directory.Exists(inputFolder))
            {
                ProcessDirectory(inputFolder, assetsFolder);
            }
            else if (File.Exists(inputFolder))
            {
                ProcessFile(inputFolder, assetsFolder);
            }
            else
            {
                Console.WriteLine($"Input folder {inputFolder} does not exist.");
                return;
            }

            WriteIndexFile(outputFolder);
            WriteImagesJsonFile(outputFolder);
            WriteWebContentFiles(outputFolder);

            Console.WriteLine($"Results are in {outputFolder}");

            if (CardboardPhotos.Count > 20)
            {
                Console.WriteLine();
                Console.WriteLine("Warning: A large number of photos (> 20) may cause instability in some browsers.");
                Console.WriteLine($"  {CardboardPhotos.Count} photos were processed.");
                Console.WriteLine("  Consider running again with a smaller set of photos.");
            }
        }

        /// <summary>
        /// Processes the directory.
        /// </summary>
        /// <param name="directoryPath">The directory path.</param>
        /// <param name="outputFolder">The output folder path.</param>
        private static void ProcessDirectory(string directoryPath, string outputFolder)
        {
            Console.WriteLine($"Processing files in folder {directoryPath}.");
            var files = Directory.GetFiles(directoryPath);

            foreach (var file in files)
            {
                if(string.Compare(Path.GetFileName(file), "title.txt", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    // If a title.txt file is present, the first line in the file
                    // should be displayed on the placard on the welcome screen.
                    var title = File.ReadLines(file).First();
                    CardboardPhotos[0].Caption = title;
                }
                else
                {
                    ProcessFile(file, outputFolder);
                }
            }
        }

        /// <summary>
        /// Processes a single Cardboard photo file
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <param name="outputFolder">The output folder path.</param>
        private static void ProcessFile(string filePath, string outputFolder)
        {
            Console.WriteLine($"Processing file {filePath}.");
            var cardboardPhoto = new CardboardPhoto(filePath);
            var fileNameNoExtension = Path.GetFileNameWithoutExtension(filePath);

            // Save the left, right, and preview photos
            cardboardPhoto.SaveLeftPhoto(Path.Combine(outputFolder, fileNameNoExtension + "_left.jpg"), true);
            cardboardPhoto.SaveRightPhoto(Path.Combine(outputFolder, fileNameNoExtension + "_right.jpg"), true);
            cardboardPhoto.SavePreview(Path.Combine(outputFolder, fileNameNoExtension + "_preview.jpg"), PreviewImageSize);

            // Generate image ids
            var imageId = $"#image{CardboardPhotos.Count}";
            cardboardPhoto.LeftImageId = $"{imageId}-left";
            cardboardPhoto.RightImageId = $"{imageId}-right";
            cardboardPhoto.PreviewImageId = $"{imageId}-preview";

            // Add to our list of photos for later processing
            CardboardPhotos.Add(cardboardPhoto);
        }

        /// <summary>
        /// Writes the index HTML file to the output folder
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void WriteIndexFile(string outputFolder)
        {
            // Build up HTML to insert into index.html
            var stringBuilderAssets = new StringBuilder();
            var stringBuilderPreview = new StringBuilder();
            var stringBuilderCarousel = new StringBuilder();
            var n = 0;
            var imageCount = CardboardPhotos.Count - 1; // One less due to start.png
            foreach (var photo in CardboardPhotos)
            {
                if (!string.IsNullOrWhiteSpace(photo.LeftImagePath) &&
                    !string.IsNullOrWhiteSpace(photo.RightImagePath))
                {
                    // left
                    var path = $"{AssetsFolderName}/{Path.GetFileName(photo.LeftImagePath)}";
                    path = Uri.EscapeUriString(path);
                    var id = photo.LeftImageId.Remove(0, 1); // Remove the leading #
                    stringBuilderAssets.Append($"\r\n          <img id=\"{id}\" src=\"{path}\">");

                    // right
                    path = $"{AssetsFolderName}/{Path.GetFileName(photo.RightImagePath)}";
                    path = Uri.EscapeUriString(path);
                    id = photo.RightImageId.Remove(0, 1); // Remove the leading #
                    stringBuilderAssets.Append($"\r\n          <img id=\"{id}\" src=\"{path}\">");

                    // preview
                    path = $"{AssetsFolderName}/{Path.GetFileName(photo.PreviewImagePath)}";
                    path = Uri.EscapeUriString(path);
                    id = photo.PreviewImageId.Remove(0, 1); // Remove the leading #
                    stringBuilderPreview.Append($"\r\n          <img id=\"{id}\" src=\"{path}\">");

                    // carousel
                    var previewImage = new PreviewImage(n, imageCount, CarouselRadius, CarouselAngleReserved, CarouselImageSpaceFraction, CarouselMaxImageSizeInMeters);
                    var navigationOrbY = CarouselHeight - (previewImage.Size / 2) - CarouselNavigationOrbRadius;
                    stringBuilderCarousel.Append($"\r\n      <a-image class=\"welcome\" position=\"{previewImage.X} {CarouselHeight} {previewImage.Z}\" rotation=\"0 {previewImage.RotationY} 0\" src=\"{photo.PreviewImageId}\" width=\"{previewImage.Size}\" height=\"{previewImage.Size}\" ></a-image>");
                    stringBuilderCarousel.Append($"\r\n      <a-sphere cursor-listener-nav=\"imageIndex: {n + 1}\" class=\"welcome cursor-active\" radius=\"{CarouselNavigationOrbRadius}\" position=\"{previewImage.X} {navigationOrbY} {previewImage.Z}\" color=\"silver\"></a-sphere>");
                    stringBuilderCarousel.Append($"\r\n      <a-plane cursor-visible class=\"welcome cursor-active\" position=\"{previewImage.X} {navigationOrbY} {previewImage.Z}\" rotation=\"0 {previewImage.RotationY} 0\" width=\"{previewImage.Size}\" height=\"{CarouselNavigationOrbRadius * 2}\" material=\"opacity: 0.0; transparent: true\"></a-plane>");

                    // Next image
                    n++;
                }
            }

            // Place the preview assets above the other assets to encourage them to load first
            var allAssets = stringBuilderPreview.ToString() + stringBuilderAssets.ToString();

            // Read in the template index.html file, and replace the placeholder DIV
            // with the html we generated above
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream("CardboardWebVR.web_template.index.html"))
            {
                using (StreamReader reader = new StreamReader(resource))
                {
                    var text = reader.ReadToEnd();
                    text = text.Replace("<div id=\"asset-placeholder\"></div>", allAssets);
                    text = text.Replace("<div id=\"carousel-placeholder\"></div>", stringBuilderCarousel.ToString());

                    // Read the welcome text and replace that too
                    using(var welcomeResource = assembly.GetManifestResourceStream("CardboardWebVR.web_template.welcome.txt"))
                    {
                        using (StreamReader welcomeReader = new StreamReader(welcomeResource))
                        {
                            var welcomeText = welcomeReader.ReadToEnd();
                            // Replace literal carriage return / line feeds with 
                            // An escape sequence. This needs to be all one line.
                            welcomeText = welcomeText.Replace("\r\n", "\\n");
                            welcomeText = welcomeText.Replace("\n", "\\n");
                            text = text.Replace("WELCOME-PLACEHOLDER", welcomeText);
                        }
                    }

                    // Replace the placard placeholder
                    text = text.Replace("PLACARD-PLACEHOLDER", CardboardPhotos[0].Caption);

                    var path = Path.Combine(outputFolder, "index.html");
                    Console.WriteLine($"Saving output file {path}");
                    File.WriteAllText(path, text);
                }
            }
        }

        /// <summary>
        /// Writes the images.json file to the output folder.
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void WriteImagesJsonFile(string outputFolder)
        {
            var path = Path.Combine(outputFolder, "images.json");
            Console.WriteLine($"Saving output file {path}");
            var jsonText = JsonConvert.SerializeObject(CardboardPhotos);
            File.WriteAllText(path, jsonText);
        }

        /// <summary>
        /// Writes the static web content to the output folder.
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void WriteWebContentFiles(string outputFolder)
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            foreach (var kvp in ResourceDictionary)
            {
                var path = Path.Combine(outputFolder, kvp.Value);
                Console.WriteLine($"Saving output file {path}");
                using (var resource = assembly.GetManifestResourceStream(kvp.Key))
                {
                    using (var file = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        resource.CopyTo(file);
                    }
                }
            }
        }

        /// <summary>
        /// Prints help information.
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("CardboardWebVR generates a WebVR site from a set of Cardboard Camera photos.");
            Console.WriteLine("usage:");
            Console.WriteLine("CardboardWebVR [input-folder] [output-folder]");
            Console.WriteLine("  [input-folder] must contain one or more Cardboard Camera photo files.");
            Console.WriteLine("                 The filenames will be dispayed in the generated site.");
            Console.WriteLine("  [output-folder] is the location where the output site will be written.");
        }
    }
}
