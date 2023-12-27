namespace TypeGen;

public static class Util
{
	public static void ThrowIfNull(this object? obj, string message)
	{
		if (obj is null)
		{
			throw new ArgumentNullException(message);
		}
	}
}
