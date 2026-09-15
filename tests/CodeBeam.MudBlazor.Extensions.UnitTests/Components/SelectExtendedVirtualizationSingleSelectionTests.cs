using AwesomeAssertions;
using Bunit;
using MudExtensions.UnitTests.Extensions;

namespace MudExtensions.UnitTests.Components
{
    [TestFixture]
    public class SelectExtendedVirtualizationSingleSelectionTests : BunitTest
    {
        [Test]
        public void VirtualizedItemCollection_InitializedSingleSelectionUsesOneShadowItem()
        {
            var items = Enumerable.Range(1, 4_000).Select(value => (int?)value).ToList();

            var cut = Context.Render<MudSelectExtended<int?>>(parameters => parameters
                .Add(x => x.ItemCollection, items)
                .Add(x => x.Virtualize, true)
                .Add(x => x.Value, 3_999));

            cut.WaitForAssertion(() =>
                cut.Instance.GetState(x => x.Text).Should().Be("3999"));

            var shadowList = cut.Find("div[style='display: none']");
            shadowList.QuerySelectorAll("div.mud-list-item-extended").Count().Should().Be(1);
        }
    }
}
