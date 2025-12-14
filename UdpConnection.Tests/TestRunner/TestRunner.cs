using System.Diagnostics;
using System.Reflection;

namespace UdpConnection.Tests.TestRunner;

/// <summary>
/// テスト実行クラス
/// </summary>
public class TestRunner
{
    private int _passCount;
    private int _failCount;
    private readonly List<(string Name, string Message)> _failures = new();

    /// <summary>
    /// 指定したアセンブリ内の全テストを実行する
    /// </summary>
    public void RunAll(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetCallingAssembly();

        var testClasses = assembly.GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(t => t.GetMethods().Any(m => m.GetCustomAttribute<TestAttribute>() != null))
            .OrderBy(t => t.Name);

        Console.WriteLine("=== Test Run Started ===\n");
        var sw = Stopwatch.StartNew();

        foreach (var testClass in testClasses)
        {
            RunTestClass(testClass);
        }

        sw.Stop();
        PrintSummary(sw.Elapsed);
    }

    /// <summary>
    /// 指定したテストクラスのテストを実行する
    /// </summary>
    public void Run<T>() where T : class
    {
        Console.WriteLine("=== Test Run Started ===\n");
        var sw = Stopwatch.StartNew();

        RunTestClass(typeof(T));

        sw.Stop();
        PrintSummary(sw.Elapsed);
    }

    private void RunTestClass(Type testClass)
    {
        Console.WriteLine($"[{testClass.Name}]");

        var testMethods = testClass.GetMethods()
            .Where(m => m.GetCustomAttribute<TestAttribute>() != null)
            .OrderBy(m => m.Name);

        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(testClass);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Failed to create instance: {ex.Message}");
            return;
        }

        foreach (var method in testMethods)
        {
            RunTestMethod(instance!, method);
        }

        Console.WriteLine();
    }

    private void RunTestMethod(object instance, MethodInfo method)
    {
        var testName = $"{instance.GetType().Name}.{method.Name}";

        try
        {
            // 戻り値がTaskの場合は非同期メソッドとして待機
            var result = method.Invoke(instance, null);
            if (result is Task task)
            {
                task.GetAwaiter().GetResult();
            }

            Console.WriteLine($"  [PASS] {method.Name}");
            _passCount++;
        }
        catch (TargetInvocationException ex)
        {
            var innerEx = ex.InnerException ?? ex;
            var message = innerEx is AssertException
                ? innerEx.Message
                : $"{innerEx.GetType().Name}: {innerEx.Message}";

            Console.WriteLine($"  [FAIL] {method.Name}");
            Console.WriteLine($"         {message}");
            _failCount++;
            _failures.Add((testName, message));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {method.Name}");
            Console.WriteLine($"         {ex.GetType().Name}: {ex.Message}");
            _failCount++;
            _failures.Add((testName, ex.Message));
        }
    }

    private void PrintSummary(TimeSpan elapsed)
    {
        Console.WriteLine("=== Test Run Completed ===");
        Console.WriteLine($"Duration: {elapsed.TotalSeconds:F2}s");
        Console.WriteLine($"Results: {_passCount} passed, {_failCount} failed");

        if (_failures.Count > 0)
        {
            Console.WriteLine("\n=== Failures ===");
            foreach (var (name, message) in _failures)
            {
                Console.WriteLine($"  {name}");
                Console.WriteLine($"    {message}");
            }
        }

        Console.WriteLine();
    }
}
