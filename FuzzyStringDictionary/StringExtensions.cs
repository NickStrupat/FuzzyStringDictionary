using System.Globalization;

static class StringExtensions
{
	public static String ToReverse(this String text) => text.AsSpan().ToReverse();
	public static String ToReverse(this ReadOnlyMemory<Char> text) => text.Span.ToReverse();
	public static String ToReverse(this ReadOnlySpan<Char> text)
	{
		var i = 0;
		Span<Char> span = stackalloc Char[text.Length];
		foreach (var grapheme in text.EnumerateGraphemes())
			grapheme.CopyTo(span.Slice(text.Length - (i += grapheme.Length)));
		return new String(span);
	}

	public static GraphemeMemoryEnumerator EnumerateGraphemes(this String text) => new(text.AsMemory());
	public static GraphemeMemoryEnumerator EnumerateGraphemes(this ReadOnlyMemory<Char> text) => new(text);
	public static GraphemeSpanEnumerator EnumerateGraphemes(this ReadOnlySpan<Char> text) => new(text);

	public ref struct GraphemeMemoryEnumerator
	{
		private ReadOnlyMemory<Char> text;
		private Int32 nextLength = 0;
		public GraphemeMemoryEnumerator(ReadOnlyMemory<Char> text) => this.text = text;
		public GraphemeMemoryEnumerator GetEnumerator() => this;

		public Boolean MoveNext()
		{
			if (nextLength >= text.Length)
				return false;
			text = text[nextLength..];
			nextLength = StringInfo.GetNextTextElementLength(text.Span);
			return true;
		}

		public ReadOnlyMemory<Char> Current => text[..nextLength];
	}

	public ref struct GraphemeSpanEnumerator
	{
		private ReadOnlySpan<Char> text;
		private Int32 nextLength = 0;
		public GraphemeSpanEnumerator(ReadOnlySpan<Char> text) => this.text = text;
		public GraphemeSpanEnumerator GetEnumerator() => this;

		public Boolean MoveNext()
		{
			if (nextLength >= text.Length)
				return false;
			text = text[nextLength..];
			nextLength = StringInfo.GetNextTextElementLength(text);
			return true;
		}

		public ReadOnlySpan<Char> Current => text[..nextLength];
	}
}
