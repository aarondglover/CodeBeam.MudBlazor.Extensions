using BenchmarkDotNet.Running;
using MudExtensions.Benchmarks;

if (args.Length > 0 && string.Equals(args[0], "probe", StringComparison.OrdinalIgnoreCase))
{
    return ScaleProbe.Run(args[1..]);
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
return 0;
