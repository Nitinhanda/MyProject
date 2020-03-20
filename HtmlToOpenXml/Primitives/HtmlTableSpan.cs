using System;

namespace HtmlToOpenXml
{
    sealed class HtmlTableSpan : IComparable<HtmlTableSpan>
    {
        public CellPosition CellOrigin;
        public int RowSpan;
        public int ColSpan;

        public HtmlTableSpan(CellPosition origin)
        {
            this.CellOrigin = origin;
        }

        public int CompareTo(HtmlTableSpan other)
        {
            if (other == null) return -1;
            int rc = this.CellOrigin.Row.CompareTo(other.CellOrigin.Row);
            if (rc != 0) return rc;
            return this.CellOrigin.Column.CompareTo(other.CellOrigin.Column);
        }
    }
}