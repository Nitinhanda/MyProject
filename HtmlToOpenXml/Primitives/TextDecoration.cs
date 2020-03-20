using System;

namespace HtmlToOpenXml
{
	/// <summary>
	/// Specifies some decoration to apply to a text.
	/// </summary>
	[Flags]
	enum TextDecoration
	{
		None = 0,
		Underline = 2,
		LineThrough = 4
	}
}