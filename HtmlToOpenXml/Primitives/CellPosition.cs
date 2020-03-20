namespace HtmlToOpenXml
{
    /// <summary>
    /// Represents the location of cell in a table (2d matrix).
    /// </summary>
    struct CellPosition
    {
        public static readonly CellPosition Empty = new CellPosition();


        /// <summary>
        /// Initializes a new instance of the <see cref='HtmlToOpenXml.CellPosition'/> class from
        /// the specified location.
        /// </summary>
        public CellPosition(int row, int column)
        {
            this.Row = row;
            this.Column = column;
        }

        /// <summary>
        /// Translates this position by the specified amount.
        /// </summary>
        public void Offset(int dr, int dc)
        {
            unchecked
            {
                Row += dr;
                Column += dc;
            }
        }

        /// <summary>
        /// Gets the horizontal coordinate of this position.
        /// </summary>
        public int Row { get; set; }

        /// <summary>
        /// Gets the vertical coordinate of this position.
        /// </summary>
        public int Column { get; set; }
    }
}