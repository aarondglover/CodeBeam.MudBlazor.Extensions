using AwesomeAssertions;
using Bunit;
using MudExtensions.UnitTests.TestComponents;

namespace MudExtensions.UnitTests.Components;

[TestFixture]
public class SelectExtendedLiveItemMutationTests : BunitTest
{
    [Test]
    public void SelectedObjectMutation_ClosedSingleSelect_UpdatesDisplayedTextOnRerender()
    {
        var items = new List<MutableSelectItem?>
        {
            new(1, "One"),
            new(2, "Two")
        };
        var selected = items[1]!;

        var cut = Context.Render<MudSelectExtended<MutableSelectItem>>(parameters => parameters
            .Add(x => x.ItemCollection, items)
            .Add(x => x.Value, selected)
            .Add(x => x.ToStringFunc, item => item?.Name));

        cut.Find("input").GetAttribute("value").Should().Be("Two");

        selected.Name = "Two updated";
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.ItemCollection, items)
            .Add(x => x.Value, selected)
            .Add(x => x.ToStringFunc, item => item?.Name));

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("Two updated"));
    }

    [Test]
    public void SelectedObjectMutation_ClosedMultiSelect_UpdatesDisplayedTextOnRerender()
    {
        var first = new MutableSelectItem(1, "One");
        var second = new MutableSelectItem(2, "Two");
        var items = new List<MutableSelectItem?> { first, second };
        var selected = new MutableSelectItem?[] { first, second };

        var cut = Context.Render<MudSelectExtended<MutableSelectItem>>(parameters => parameters
            .Add(x => x.ItemCollection, items)
            .Add(x => x.MultiSelection, true)
            .Add(x => x.SelectedValues, selected)
            .Add(x => x.ToStringFunc, item => item?.Name));

        cut.Find("input").GetAttribute("value").Should().Be("One, Two");

        second.Name = "Two updated";
        cut.SetParametersAndRender(parameters => parameters
            .Add(x => x.ItemCollection, items)
            .Add(x => x.MultiSelection, true)
            .Add(x => x.SelectedValues, selected)
            .Add(x => x.ToStringFunc, item => item?.Name));

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("One, Two updated"));
    }

    [Test]
    public async Task MutableItemCollection_WhileOpen_ReflectsMutationRemovalAndAddition()
    {
        var cut = Context.Render<SelectMutableItemCollectionRefreshTest>();

        cut.Find("div.mud-input-control").Click();
        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll("div.mud-list-item-extended");
            items.Should().HaveCount(3);
            items[0].TextContent.Should().Contain("One");
        });

        await cut.InvokeAsync(() => cut.Instance.RenameFirst("One updated"));

        cut.WaitForAssertion(() =>
            cut.FindAll("div.mud-list-item-extended")[0].TextContent.Should().Contain("One updated"));

        await cut.InvokeAsync(cut.Instance.RemoveLast);

        cut.WaitForAssertion(() =>
            cut.FindAll("div.mud-list-item-extended").Should().HaveCount(2));

        await cut.InvokeAsync(() => cut.Instance.Add("Four"));

        cut.WaitForAssertion(() =>
        {
            var items = cut.FindAll("div.mud-list-item-extended");
            items.Should().HaveCount(3);
            items[^1].TextContent.Should().Contain("Four");
        });
    }
}
