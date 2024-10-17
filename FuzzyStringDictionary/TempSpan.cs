using System.Buffers;
using System.Runtime.CompilerServices;

namespace NickStrupat;

readonly ref struct TempSpan<T>
{
	private readonly T[]? array;
	public readonly Span<T> Data;

	public TempSpan(Int32 length)
	{
		array = ArrayPool<T>.Shared.Rent(length);
		Data = array.AsSpan(0, length);
	}

	public TempSpan(Span<T> data)
	{
		array = null;
		Data = data;
	}

	public void Dispose()
	{
		if (array != null)
			ArrayPool<T>.Shared.Return(array);
	}

	public static Boolean ShouldRent(Int32 lengthOfTs, Int32 maxStackAllocBytes = 1024) => lengthOfTs * Unsafe.SizeOf<T>() > maxStackAllocBytes;
}
