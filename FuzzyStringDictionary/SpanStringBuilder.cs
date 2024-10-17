namespace NickStrupat;

ref struct SpanStringBuilder
{
	private readonly Span<Char> buffer;
	private Int32 index = 0;
	public SpanStringBuilder(Span<Char> buffer) => this.buffer = buffer;

	public void Append(ReadOnlySpan<Char> value)
	{
		value.CopyTo(buffer[index..]);
		index += value.Length;
	}

	public override String ToString() => Span.ToString();
	public ReadOnlySpan<Char> Span => buffer[..index];
}
