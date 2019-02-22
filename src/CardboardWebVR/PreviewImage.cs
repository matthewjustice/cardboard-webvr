// <copyright file="PreviewImage.cs" company="Matthew Justice">
//     Copyright (c) Matthew Justice. All rights reserved.
// </copyright>

namespace CardboardWebVR
{
    using System;

    /// <summary>
    /// Describes the properties of a preview image
    /// </summary>
    public class PreviewImage
    {
        /// <summary>
        /// The maximum image size
        /// </summary>
        private const double MaxImageSize = 1.0;

        /// <summary>
        /// The image percentage of the space it could fill
        /// </summary>
        private const double ImagePercentage = 0.85;

        /// <summary>
        /// The nth preview image
        /// </summary>
        private readonly int n;

        /// <summary>
        /// The image count
        /// </summary>
        private readonly int imageCount;

        /// <summary>
        /// The radius of the circle
        /// </summary>
        private readonly double radius;

        /// <summary>
        /// The angle reserved for other content
        /// </summary>
        private readonly double angleReserved;

        /// <summary>
        /// The angle of this image, measured clockwise starting at negative z axis
        /// </summary>
        private readonly double angleImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="PreviewImage"/> class.
        /// </summary>
        /// <param name="n">The nth image</param>
        /// <param name="imageCount">The image count.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="angleReserved">The angle reserved, in degrees</param>
        public PreviewImage(int n, int imageCount, double radius, double angleReserved)
        {
            this.n = n;
            this.imageCount = imageCount;
            this.radius = radius;
            this.angleReserved = angleReserved;

            var angleAvailableForUse = 360 - this.angleReserved;
            var angleBetweenImages = angleAvailableForUse / (imageCount - 1);
            var angleStartOffset = angleReserved / 2;
            this.angleImage = ((double)n * angleBetweenImages) + angleStartOffset;

            ////Console.WriteLine($"n={this.n}, imageCount ={this.imageCount}, radius={this.radius}, angleAvailableForUse={angleAvailableForUse}, angleBetweenImages={angleBetweenImages},angleStartOffset={angleStartOffset},angleImage={this.angleImage}");
        }

        /// <summary>
        /// Gets the x coordinate of the preview image
        /// </summary>
        /// <value>
        /// The x coordinate of the preview image
        /// </value>
        public double X
        {
            get
            {
                var angleAdjusted = this.AdjustAngle(this.angleImage);
                var angleRadians = Math.PI / 180 * angleAdjusted;
                var x = this.radius * Math.Cos(angleRadians);

                // If the value is fractionally very small just make it zero
                if ((x > 0 && x < .001) || (x < 0 && x > -0.001))
                {
                    x = 0.0;
                }

                return x;
            }
        }

        /// <summary>
        /// Gets the z coordinate of the preview image
        /// </summary>
        /// <value>
        /// The z coordinate of the preview image
        /// </value>
        public double Z
        {
            get
            {
                var angleAdjusted = this.AdjustAngle(this.angleImage);
                var angleRadians = Math.PI / 180 * angleAdjusted;
                var z = -1 * this.radius * Math.Sin(angleRadians);

                // If the value is fractionally very small just make it zero
                if ((z > 0 && z < .001) || (z < 0 && z > -0.001))
                {
                    z = 0.0;
                }

                return z;
            }
        }

        /// <summary>
        /// Gets the rotation in degrees around the y axis
        /// </summary>
        /// <value>
        /// The rotation in degrees around the y axis
        /// </value>
        public double RotationY
        {
            get
            {
                return -1 * this.angleImage;
            }
        }

        /// <summary>
        /// Gets the size (width and height) of the square image
        /// </summary>
        /// <value>
        /// The size (width and height) of the square image
        /// </value>
        public double Size
        {
            get
            {
                // Find the usable part of the circumference of the circle
                var circumference = 2 * Math.PI * this.radius;
                var usablePercent = (360 - this.angleReserved) / 360;
                var usableSize = circumference * usablePercent;

                // The size of the image will be a portion of the overall usable size
                var size = usableSize / this.imageCount * ImagePercentage;
                if (size > MaxImageSize)
                {
                    size = MaxImageSize;
                }

                return size;
            }
        }

        /// <summary>
        /// Adjusts the angle.
        /// We general treat the angle as clockwise from the z axis,
        /// but to find the coordinates we need the angle
        /// as counterclockwise from the x axis.
        /// </summary>
        /// <param name="angle">The angle.</param>
        /// <returns>The adjusted angle</returns>
        private double AdjustAngle(double angle)
        {
            return 90 - angle;
        }
    }
}