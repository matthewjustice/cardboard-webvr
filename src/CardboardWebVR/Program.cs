// <copyright file="Program.cs" company="Matthew Justice">
//     Copyright (c) Matthew Justice. All rights reserved.
// </copyright>

namespace CardboardWebVR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
        /// The resource dictionary includes a list of static resources
        /// that will need to be written to disk. The key is the resource
        /// name and the value is the relative path where it should be written.
        /// </summary>
        private static readonly Dictionary<string, string> ResourceDictionary = new Dictionary<string, string>
        {
            { "CardboardWebVR.web_template.scripts.aframe-stereo-component.min.js", $"{ScriptsFolderName}/aframe-stereo-component.min.js" },
            { "CardboardWebVR.web_template.scripts.slideshow.js", $"{ScriptsFolderName}/slideshow.js" },
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

            SaveIndex(outputFolder);
            SaveImagesJson(outputFolder);
            SaveWebContent(outputFolder);

            Console.WriteLine($"Results are in {outputFolder}");
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
                ProcessFile(file, outputFolder);
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

            // Save the left and right photos
            cardboardPhoto.SaveLeftPhoto(Path.Combine(outputFolder, fileNameNoExtension + "_left.jpg"), true);
            cardboardPhoto.SaveRightPhoto(Path.Combine(outputFolder, fileNameNoExtension + "_right.jpg"), true);

            // Generate image ids
            var imageId = $"#image{CardboardPhotos.Count + 1}";
            cardboardPhoto.LeftImageId = $"{imageId}-left";
            cardboardPhoto.RightImageId = $"{imageId}-right";

            // Add to our list of photos for later processing
            CardboardPhotos.Add(cardboardPhoto);
        }

        /// <summary>
        /// Saves the index HTML file
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void SaveIndex(string outputFolder)
        {
            // Build up HTML to insert into index.html
            var sb = new StringBuilder();
            foreach (var photo in CardboardPhotos)
            {
                if (!string.IsNullOrWhiteSpace(photo.LeftImagePath) &&
                    !string.IsNullOrWhiteSpace(photo.RightImagePath))
                {
                    // left
                    var path = $"{AssetsFolderName}/{Path.GetFileName(photo.LeftImagePath)}";
                    var id = photo.LeftImageId.Remove(0, 1); // Remove the leading #
                    sb.Append($"          <img id=\"{id}\" src=\"{path}\">\r\n");

                    // right
                    path = $"{AssetsFolderName}/{Path.GetFileName(photo.RightImagePath)}";
                    id = photo.RightImageId.Remove(0, 1); // Remove the leading #
                    sb.Append($"          <img id=\"{id}\" src=\"{path}\">\r\n");
                }
            }

            // Read in the template index.html file, and replace the placeholder DIV
            // with the html we generated above
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            using (var resource = assembly.GetManifestResourceStream("CardboardWebVR.web_template.index.html"))
            {
                using (StreamReader reader = new StreamReader(resource))
                {
                    var text = reader.ReadToEnd();
                    text = text.Replace("          <div id=\"asset-placeholder\"></div>", sb.ToString());
                    var path = Path.Combine(outputFolder, "index.html");
                    Console.WriteLine($"Saving output file {path}");
                    File.WriteAllText(path, text);
                }
            }
        }

        /// <summary>
        /// Saves the images.json file.
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void SaveImagesJson(string outputFolder)
        {
            var path = Path.Combine(outputFolder, "images.json");
            Console.WriteLine($"Saving output file {path}");
            var jsonText = JsonConvert.SerializeObject(CardboardPhotos);
            File.WriteAllText(path, jsonText);
        }

        /// <summary>
        /// Saves the static web content to the output folder
        /// </summary>
        /// <param name="outputFolder">The output folder.</param>
        private static void SaveWebContent(string outputFolder)
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
