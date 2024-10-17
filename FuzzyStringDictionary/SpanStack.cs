using System.Collections;

namespace NickStrupat;

ref struct SpanStack<T>
{
	private readonly Span<T> buffer;
	private Int32 index = 0;
	public SpanStack(Span<T> buffer) => this.buffer = buffer;

	public void Push(T item) => buffer[index++] = item;
	public T Pop() => buffer[--index];
	public ref readonly T Peek() => ref buffer[index - 1];
	public ref readonly T this[Int32 i] => ref Span[i];
	public Boolean IsEmpty => index == 0;
	public Int32 Count => index;
	public Int32 Capacity => buffer.Length;
	public void Clear() => index = 0;
	public ReadOnlySpan<T> Span => buffer[..index];
}
