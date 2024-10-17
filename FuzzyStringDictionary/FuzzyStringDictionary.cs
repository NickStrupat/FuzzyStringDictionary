using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;

namespace NickStrupat;

public sealed class FuzzyStringDictionary
{
	private readonly Dictionary<Int32, Strings> dictionary = new();
	private readonly Int32 maxEditDistance;
	private readonly StringComparison stringComparison;

	public FuzzyStringDictionary(UInt32 maxEditDistance, StringComparison stringComparison = StringComparison.CurrentCultureIgnoreCase)
	{
		this.maxEditDistance = checked((Int32)maxEditDistance);
		this.stringComparison = stringComparison;
	}

	public void Add(String text)
	{
		var chcText = new CachedHashCode<String>(text);

		ForEachDeletionHashCode(text, hash => dictionary.Add(hash, chcText)
			#if DEBUG
			, debugInfo => debugDictionary.Add(debugInfo, chcText)
			#endif
		);
	}

	public void Remove(String text)
	{
		var chcText = new CachedHashCode<String>(text);

		ForEachDeletionHashCode(text, hash => dictionary.Remove(hash, chcText)
			#if DEBUG
			, debugInfo => debugDictionary.Remove(debugInfo, chcText)
			#endif
		);
	}

	public HashSet<String> Lookup(String text)
	{
		HashSet<String> matches = new();
		ForEachDeletionHashCode(text,
			hash =>
			{
				if (dictionary.TryGetValue(hash, out var values))
					matches.UnionWith(values.Select(x => x.Value));
			});
		return matches;
	}

	private static Int32 RotateLeft(Int32 value, Int32 offset) => unchecked((Int32)BitOperations.RotateLeft(unchecked((UInt32) value), offset));

	private void ForEachDeletionHashCode(String text, Action<Int32> hashCodeAction
		#if DEBUG
		, Action<DebugInfo>? textAction = null
		#endif
	)
	{
		var hashCode = 0;
		var len = text.Length;
		using TempSpan<Int32> graphemeHashCodesSpan = TempSpan<Int32>.ShouldRent(len) ? new(len) : new(stackalloc Int32[len]);
		SpanStack<Int32> graphemeHashCodes = new(graphemeHashCodesSpan.Data);
		var graphemeHashCodeCounts = new Dictionary<Int32, Int32>(text.Length);
		#if DEBUG
		List<ReadOnlyMemory<Char>> graphemes = new(text.Length);
		#endif
		foreach (var grapheme in text.EnumerateGraphemes()) // index grapheme hashes and accumulate hash code for the whole string
		{
			var ghc = String.GetHashCode(grapheme.Span, stringComparison);
			var rlo = CollectionsMarshal.GetValueRefOrAddDefault(graphemeHashCodeCounts, ghc, out _)++;
			ghc = RotateLeft(ghc, rlo); // rotate hash code by the number of graphemes already hashed, otherwise the hash code would be the same for "abc" and "cba"
			graphemeHashCodes.Push(ghc);
			#if DEBUG
			graphemes.Add(grapheme);
			#endif
			hashCode += ghc;
		}

		hashCodeAction(hashCode);
		var k = Math.Min(graphemeHashCodes.Count - 1, maxEditDistance);

		#if DEBUG
		var hold = hashCode;
		textAction?.Invoke(new(text, hashCode));
		Stack<Int32> deletionIndexes = new(k);
		#endif

		ForEachKCombinationOfN(graphemeHashCodes.Span, k,
			(graphemeHashCodes, i) =>
			{
				hashCodeAction(hashCode -= graphemeHashCodes[i]);

				#if DEBUG
				deletionIndexes.Push(i);
				using TempSpan<Char> stringBuilderSpan = TempSpan<Char>.ShouldRent(text.Length) ? new(text.Length) : new(stackalloc Char[text.Length]);
				SpanStringBuilder sb = new(stringBuilderSpan.Data);
				for (var i1 = 0; i1 < graphemes.Count; i1++)
				{
					if (!deletionIndexes.Contains(i1))
						sb.Append(graphemes[i1].Span);
				}
				textAction?.Invoke(new(sb.ToString(), hashCode));
				#endif
			},
			(graphemeHashCodes, i) =>
			{
				hashCode += graphemeHashCodes[i];
				#if DEBUG
				deletionIndexes.Pop();
				#endif
			});

		#if DEBUG
		if (hashCode != hold)
			throw new Exception("Hash code mismatch after deletion roundtrip.");
#endif
	}

	private static void ForEachKCombinationOfN(ReadOnlySpan<Int32> span, Int32 k, ReadOnlySpanAction<Int32, Int32> pushed, ReadOnlySpanAction<Int32, Int32> popped)
	{
		using TempSpan<Int32> stackSpan = TempSpan<Int32>.ShouldRent(k) ? new(k) : new(stackalloc Int32[k]);
		SpanStack<Int32> stack = new(stackSpan.Data);
		ForEachKCombinationOfNUtil(span, 1, k, stack, pushed, popped);

		static void ForEachKCombinationOfNUtil(ReadOnlySpan<Int32> span, Int32 left, Int32 k, SpanStack<Int32> stack, ReadOnlySpanAction<Int32, Int32> pushed, ReadOnlySpanAction<Int32, Int32> popped)
		{
			if (k == 0)
				return;
			for (var i = left; i <= span.Length; ++i)
			{
				stack.Push(i - 1);
				pushed(span, stack.Peek());
				ForEachKCombinationOfNUtil(span, i + 1, k - 1, stack, pushed, popped);
				popped(span, stack.Peek());
				stack.Pop();
			}
		}
	}

	private sealed class Strings : HashSet<CachedHashCode<String>> { public override String ToString() => $"Count = {Count}, {{\"{String.Join("\", \"", this.Take(5))}\"{(Count > 5 ? ", ..." : "")}}}"; }

	#if DEBUG
	private readonly struct DebugInfo : IEquatable<DebugInfo>
	{
		public readonly Int32 HashCode;
		public readonly String Text;
		public DebugInfo(String text, Int32 hashCode) { HashCode = hashCode; Text = text; }
		public override Int32 GetHashCode() => HashCode;
		public override Boolean Equals(Object? obj) => obj is DebugInfo other && Equals(other);
		public Boolean Equals(DebugInfo other) => other.HashCode == HashCode;
		public override String? ToString() => Text;
	}
	private readonly Dictionary<DebugInfo, Strings> debugDictionary = new();
	#endif
}
