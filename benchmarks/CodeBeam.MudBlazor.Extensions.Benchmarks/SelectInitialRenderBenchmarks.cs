using BenchmarkDotNet.Attributes;
using Bunit;

namespace MudExtensions.Benchmarks;

[MemoryDiagnoser]
[BenchmarkCategory("MudSelectExtended", "InitialRender")]
public class SelectInitialRenderBenchmarks
{
    private List<int?> _items = null!;
    private int?[] _selectedValues = null!;

    [Params(10, 100, 1_000, 4_000)]
    public int ItemCount { get; set; }

    [Params(false, true)]
    public bool Virtualize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _items = Enumerable.Range(1, ItemCount).Select(static value => (int?)value).ToList();
        _selectedValues = [1, ItemCount];
    }

    [Benchmark]
    public int RenderSelect()
    {
        using var context = BenchmarkBunitContext.Create();
        using var cut = context.Render<SelectBenchmarkHost>(parameters => parameters
            .Add(x => x.Items, _items)
            .Add(x => x.SelectedValues, _selectedValues)
            .Add(x => x.Virtualize, Virtualize));

        return cut.RenderCount;
    }
}
