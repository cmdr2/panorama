using System;

public static class Base58 {
    private const string DIGITS = "123456789abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ";

	public static string Encode(long intData) {
		double baseCount = DIGITS.Length;
		string result = string.Empty;
		long div = intData;

		while (intData >= baseCount) {
			div = (long)Math.Floor (intData / baseCount);
			int mod = (int)(intData - baseCount * div);
			result = DIGITS[mod] + result;
			intData = div;
		}

    	return (div != 0 ? DIGITS[(int)div] + result : result);
    }
}
