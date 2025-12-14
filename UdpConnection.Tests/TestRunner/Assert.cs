namespace UdpConnection.Tests.TestRunner;

/// <summary>
/// テスト用アサーションクラス
/// </summary>
public static class Assert
{
    public static void AreEqual<T>(T expected, T actual, string? message = null)
    {
        if (!Equals(expected, actual))
        {
            throw new AssertException(
                message ?? $"Expected: {expected}, Actual: {actual}");
        }
    }

    public static void AreNotEqual<T>(T notExpected, T actual, string? message = null)
    {
        if (Equals(notExpected, actual))
        {
            throw new AssertException(
                message ?? $"Expected not equal to: {notExpected}, but was equal");
        }
    }

    public static void IsTrue(bool condition, string? message = null)
    {
        if (!condition)
        {
            throw new AssertException(message ?? "Expected: true, Actual: false");
        }
    }

    public static void IsFalse(bool condition, string? message = null)
    {
        if (condition)
        {
            throw new AssertException(message ?? "Expected: false, Actual: true");
        }
    }

    public static void IsNull(object? obj, string? message = null)
    {
        if (obj != null)
        {
            throw new AssertException(message ?? $"Expected: null, Actual: {obj}");
        }
    }

    public static void IsNotNull(object? obj, string? message = null)
    {
        if (obj == null)
        {
            throw new AssertException(message ?? "Expected: not null, Actual: null");
        }
    }

    public static void Throws<T>(Action action, string? message = null) where T : Exception
    {
        try
        {
            action();
            throw new AssertException(
                message ?? $"Expected exception: {typeof(T).Name}, but no exception was thrown");
        }
        catch (T)
        {
            // 期待通りの例外
        }
        catch (Exception ex)
        {
            throw new AssertException(
                message ?? $"Expected exception: {typeof(T).Name}, Actual: {ex.GetType().Name}");
        }
    }

    public static void InRange(double value, double min, double max, string? message = null)
    {
        if (value < min || value > max)
        {
            throw new AssertException(
                message ?? $"Expected: {min} <= value <= {max}, Actual: {value}");
        }
    }

    public static void AreEqualWithTolerance(double expected, double actual, double tolerance, string? message = null)
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new AssertException(
                message ?? $"Expected: {expected} (±{tolerance}), Actual: {actual}");
        }
    }
}

/// <summary>
/// アサーション失敗時の例外
/// </summary>
public class AssertException : Exception
{
    public AssertException(string message) : base(message)
    {
    }
}
