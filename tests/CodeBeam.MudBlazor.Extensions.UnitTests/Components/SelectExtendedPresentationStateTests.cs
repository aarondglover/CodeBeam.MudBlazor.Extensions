using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;

namespace MudExtensions.UnitTests.Components
{
    [TestFixture]
    public class SelectExtendedPresentationStateTests : BunitTest
    {
        [Test]
        public void DeclarativeItems_RegisterMetadataWithoutHiddenList()
        {
            RenderFragment childContent = builder =>
            {
                builder.OpenComponent<MudSelectItemExtended<string>>(0);
                builder.AddAttribute(1, nameof(MudSelectItemExtended<string>.Value), "two");
                builder.AddAttribute(2, nameof(MudSelectItemExtended<string>.Text), "Friendly two");
                builder.CloseComponent();
            };

            var cut = Context.Render<MudSelectExtended<string>>(parameters => parameters
                .Add(x => x.Value, "two")
                .Add(x => x.ChildContent, childContent));

            cut.WaitForAssertion(() =>
                cut.Find("input").Attributes["value"]?.Value.Should().Be("Friendly two"));

            cut.Instance.Items.Should().ContainSingle();
            cut.FindComponents<MudListExtended<string>>().Should().BeEmpty();
            cut.FindComponents<MudSelectItemExtended<string>>().Should().ContainSingle();
        }

        [Test]
        public async Task ItemCollection_SelectOptionByIndex_UsesLogicalCollection()
        {
            var items = new List<string?> { "one", "two", "three" };

            var cut = Context.Render<MudSelectExtended<string?>>(parameters => parameters
                .Add(x => x.ItemCollection, items)
                .Add(x => x.Virtualize, true));

            await cut.InvokeAsync(() => cut.Instance.SelectOption(1));

            cut.WaitForAssertion(() =>
                cut.Find("input").Attributes["value"]?.Value.Should().Be("two"));
            cut.Instance.Items.Should().BeEmpty();
        }
    }
}
