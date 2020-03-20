namespace HtmlToOpenXml
{
    /// <summary>
    /// Represents a dimension in 2D coordinate space.
    /// </summary>
    public struct Size
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='HtmlToOpenXml.Size'/> class.
        /// </summary>
        public static readonly Size Empty = new Size();

        /// <summary>
        /// Initializes a new instance of the <see cref='HtmlToOpenXml.Size'/> class from
        /// the specified dimensions.
        /// </summary>
        public Size(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }

        /// <summary>
        /// Tests whether this size has zero width and height.
        /// </summary>
        public bool IsEmpty => Width == 0 && Height == 0;

        /// <summary>
        /// Represents the horizontal component of this size.
        /// </summary>
        public int Width { get; set; }

        /// <summary>
        /// Represents the vertical component of this size.
        /// </summary>
        public int Height { get; set; }
    }
}