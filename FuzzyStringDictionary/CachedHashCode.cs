namespace NickStrupat;

readonly struct CachedHashCode<T> : IEquatable<CachedHashCode<T>> where T : notnull
{
	private readonly Int32 hashCode;
	public readonly T Value;
	public CachedHashCode(T value) : this(value, value.GetHashCode()) {}
	public CachedHashCode(T value, Int32 hashCode) { this.hashCode = hashCode; Value = value; }
	public override Int32 GetHashCode() => hashCode;
	public override Boolean Equals(Object? obj) => obj is CachedHashCode<T> other && Equals(other);
	public Boolean Equals(CachedHashCode<T> other) => other.hashCode == hashCode && Value.Equals(other.Value);
	public override String? ToString() => Value.ToString();

	public static implicit operator CachedHashCode<T>(T value) => new(value);
	public static implicit operator T(CachedHashCode<T> value) => value.Value;
}
