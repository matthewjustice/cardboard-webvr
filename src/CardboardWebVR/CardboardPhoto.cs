// <copyright file="CardboardPhoto.cs" company="Matthew Justice">
//     Copyright (c) Matthew Justice. All rights reserved.
// </copyright>

namespace CardboardWebVR
{
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using MetadataExtractor;
    using MetadataExtractor.Formats.Xmp;
    using Newtonsoft.Json;

    /// <summary>
    /// A cardboard photo
    /// </summary>
    public class CardboardPhoto
    {
        /// <summary>
        /// The JPEG compression percentage value
        /// </summary>
        private const long JpegCompression = 90;

        /// <summary>
        /// A base64 encoded string that represents the right eye photo
        /// </summary>
        private string base64RightPhoto;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardboardPhoto"/> class.
        /// </summary>
        public CardboardPhoto()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CardboardPhoto"/> class.
        /// </summary>
        /// <param name="filePath">The file path.</param>
        /// <exception cref="Exception">Specified image file does not contain cardboard metadata</exception>
        public CardboardPhoto(string filePath)
        {
            this.SourceFilePath = filePath;

            // Get the right eye photo data from the file's metadata
            this.base64RightPhoto = GetXmpData("GImage:Data", this.SourceFilePath);
            if (string.IsNullOrWhiteSpace(this.base64RightPhoto))
            {
                throw new Exception("Specified image file does not contain cardboard metadata");
            }

            // Try to get the caption from metadata
            this.Caption = GetXmpData("dc:title[1]", this.SourceFilePath);
            if (string.IsNullOrWhiteSpace(this.Caption))
            {
                this.Caption = GetXmpData("dc:description[1]", this.SourceFilePath);
            }

            // If that failed, use the filename
            if (string.IsNullOrWhiteSpace(this.Caption))
            {
                this.Caption = Path.GetFileNameWithoutExtension(this.SourceFilePath);

                // Cardboard photos by default end with .vr.jpg, so we need an extra step
                // to get rid of the .vr text too.
                if (this.Caption.EndsWith(".vr"))
                {
                    this.Caption = this.Caption.Substring(0, this.Caption.Length - 3);
                }
            }
        }

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
        /// <value>
        /// The file path.
        /// </value>
        [JsonIgnore]
        public string SourceFilePath { get; set; }

        /// <summary>
        /// Gets or sets the left image path.
        /// </summary>
        /// <value>
        /// The left image path.
        /// </value>
        [JsonIgnore]
        public string LeftImagePath { get; set; }

        /// <summary>
        /// Gets or sets the right image path.
        /// </summary>
        /// <value>
        /// The right image path.
        /// </value>
        [JsonIgnore]
        public string RightImagePath { get; set; }

        /// <summary>
        /// Gets or sets the preview image path.
        /// </summary>
        /// <value>
        /// The preview image path.
        /// </value>
        [JsonIgnore]
        public string PreviewImagePath { get; set; }

        /// <summary>
        /// Gets or sets the left image identifier.
        /// </summary>
        /// <value>
        /// The left image identifier.
        /// </value>
        [JsonProperty("leftImageId")]
        public string LeftImageId { get; set; }

        /// <summary>
        /// Gets or sets the right image identifier.
        /// </summary>
        /// <value>
        /// The right image identifier.
        /// </value>
        [JsonProperty("rightImageId")]
        public string RightImageId { get; set; }

        /// <summary>
        /// Gets or sets the preview image identifier.
        /// </summary>
        /// <value>
        /// The preview image identifier.
        /// </value>
        [JsonIgnore]
        public string PreviewImageId { get; set; }

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>
        /// The caption.
        /// </value>
        [JsonProperty("caption")]
        public string Caption { get; set; }

        /// <summary>
        /// Saves the left eye photo.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        /// <param name="makeEquirectangular">if set to <c>true</c> [make equirectangular].</param>
        public void SaveLeftPhoto(string outputFilePath, bool makeEquirectangular)
        {
            var bitmap = (Bitmap)Image.FromFile(this.SourceFilePath).Clone();
            if (!makeEquirectangular)
            {
                bitmap.Save(outputFilePath);
            }
            else
            {
                var equirectangularBitmap = MakeEquirectangularBitmap(bitmap);
                SaveAsJpeg(equirectangularBitmap, JpegCompression, outputFilePath);
            }

            this.LeftImagePath = outputFilePath;
        }

        /// <summary>
        /// Saves the right eye photo.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        /// <param name="makeEquirectangular">if set to <c>true</c> [make equirectangular].</param>
        public void SaveRightPhoto(string outputFilePath, bool makeEquirectangular)
        {
            var bytes = GetBytesFromBase64(this.base64RightPhoto);
            if (!makeEquirectangular)
            {
                using (var outputFile = new FileStream(outputFilePath, FileMode.Create))
                {
                    outputFile.Write(bytes, 0, bytes.Length);
                    outputFile.Flush();
                }
            }
            else
            {
                using (var memoryStream = new MemoryStream(bytes))
                {
                    var bitmap = new Bitmap(memoryStream);
                    var equirectangularBitmap = MakeEquirectangularBitmap(bitmap);
                    SaveAsJpeg(equirectangularBitmap, JpegCompression, outputFilePath);
                }
            }

            this.RightImagePath = outputFilePath;
        }

        /// <summary>
        /// Saves a preview image.
        /// </summary>
        /// <param name="outputFilePath">The output file path.</param>
        /// <param name="targetSize">The desired width and height</param>
        public void SavePreview(string outputFilePath, int targetSize)
        {
            var bitmap = (Bitmap)Image.FromFile(this.SourceFilePath);
            var size = bitmap.Height;
            var x = (bitmap.Width / 2) - (size / 2);
            var cropRectange = new Rectangle(x, 0, size, size);
            var previewBitmap = bitmap.Clone(cropRectange, bitmap.PixelFormat);
            var resizedBitmap = new Bitmap(previewBitmap, new Size(targetSize, targetSize));
            SaveAsJpeg(resizedBitmap, JpegCompression, outputFilePath);
            this.PreviewImagePath = outputFilePath;
        }

        /// <summary>
        /// Saves a bitmap image as a JPEG encoded file.
        /// </summary>
        /// <param name="bitmap">The bitmap.</param>
        /// <param name="compression">The compression.</param>
        /// <param name="fileName">Name of the file.</param>
        private static void SaveAsJpeg(Bitmap bitmap, long compression, string fileName)
        {
            var jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var encoder = System.Drawing.Imaging.Encoder.Quality;
            var encoderParameters = new EncoderParameters(1);
            var encoderParameter = new EncoderParameter(encoder, compression);
            encoderParameters.Param[0] = encoderParameter;
            bitmap.Save(fileName, jpgEncoder, encoderParameters);
        }

        /// <summary>
        /// Gets the encoder.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>The image code info for the encoder format</returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }

            return null;
        }

        /// <summary>
        /// Converts a bitmap to an equirectangular bitmap
        /// </summary>
        /// <param name="originalBitmap">The original bitmap.</param>
        /// <returns>An equirectangular bitmap</returns>
        private static Bitmap MakeEquirectangularBitmap(Bitmap originalBitmap)
        {
            // An equirectangular image should be 2:1
            var equirectWidth = originalBitmap.Width;
            var equirectHeight = originalBitmap.Width / 2;
            var equirectBitmap = new Bitmap(equirectWidth, equirectHeight);
            using (var graphics = Graphics.FromImage(equirectBitmap))
            {
                graphics.Clear(Color.Black);
                int x = (equirectBitmap.Width - originalBitmap.Width) / 2;
                int y = (equirectBitmap.Height - originalBitmap.Height) / 2;
                graphics.DrawImage(originalBitmap, x, y);
            }

            return equirectBitmap;
        }

        /// <summary>
        /// Gets the XMP data for a certain property from a file
        /// </summary>
        /// <param name="property">The name of the XML property</param>
        /// <param name="filePath">The file path.</param>
        /// <returns>A string value for the first matching XMP property</returns>
        private static string GetXmpData(string property, string filePath)
        {
            var data = string.Empty;
            var metadataDirectories = ImageMetadataReader.ReadMetadata(filePath);
            var xmpDirectories = metadataDirectories.Where(d => d is XmpDirectory);

            foreach (var directory in xmpDirectories)
            {
                var xmpDirectory = (XmpDirectory)directory;
                var xmpDictionary = xmpDirectory.GetXmpProperties();
                if (xmpDictionary.TryGetValue(property, out data))
                {
                    break;
                }
            }

            return data;
        }

        /// <summary>
        /// Converts a base 64 string to an array of bytes
        /// </summary>
        /// <param name="base64string">The base64string.</param>
        /// <returns>An array of bytes</returns>
        private static byte[] GetBytesFromBase64(string base64string)
        {
            // A proper base-64 string should be a multiple of 4.
            // If it isn't, then append "=" as needed to the end
            var mod4 = base64string.Length % 4;
            if (mod4 > 0)
            {
                base64string += new string('=', 4 - mod4);
            }

            return Convert.FromBase64String(base64string);
        }
    }
}