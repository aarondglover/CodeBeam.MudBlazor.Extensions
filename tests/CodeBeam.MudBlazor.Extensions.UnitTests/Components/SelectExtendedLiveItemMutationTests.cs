using AwesomeAssertions;
using Bunit;
using MudExtensions.UnitTests.TestComponents;

namespace MudExtensions.UnitTests.Components;

[TestFixture]
public class SelectExtendedLiveItemMutationTests : BunitTest
{
    [Test]
    public async Task SelectedObjectMutation_ClosedSingleSelect_UpdatesDisplayedTextOnRerender()
    {
        var cut = Context.Render<SelectMutableItemCollectionRefreshTest>();

        cut.Find("input").GetAttribute("value").Should().Be("Two");

        await cut.InvokeAsync(() => cut.Instance.RenameSelected("Two updated"));

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("Two updated"));
    }

    [Test]
    public async Task SelectedObjectMutation_ClosedMultiSelect_UpdatesDisplayedTextOnRerender()
    {
        var cut = Context.Render<SelectMutableItemCollectionRefreshTest>();

        await cut.InvokeAsync(cut.Instance.SelectFirstTwo);
        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("One, Two"));

        await cut.InvokeAsync(() => cut.Instance.RenameSecond("Two updated"));

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("One, Two updated"));
    }

    [Test]
    public async Task ComparerMatchedSelection_UsesCurrentCollectionObjectForDisplay()
    {
        var cut = Context.Render<SelectMutableItemCollectionRefreshTest>();

        await cut.InvokeAsync(cut.Instance.UseComparerMatchedExternalSelection);

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("Two"));

        await cut.InvokeAsync(() => cut.Instance.RenameFirst("Two updated"));

        cut.WaitForAssertion(() =>
            cut.Find("input").GetAttribute("value").Should().Be("Two updated"));
    }

    [Test]
    public async Task ItemCollectionReplacement_WhileOpen_UpdatesVisibleItems()
    {
        var cut = Context.Render<SelectMutableItemCollectionRefreshTest>();

        cut.Find("div.mud-input-control").Click();
        cut.WaitForAssertion(() =>
            cut.FindAll("div.mud-list-item-extended").Should().HaveCount(3));

        await cut.InvokeAsync(cut.Instance.ReplaceCollection);

        cut.WaitForAssertion(() =>
        {
            var visibleItems = cut.FindAll("div.mud-list-item-extended");
            visibleItems.Should().HaveCount(3);
            visibleItems[0].TextContent.Should().Contain("Alpha");
            visibleItems[^1].TextContent.Should().Contain("Gamma");
        });
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
