using Bunit;
using AwesomeAssertions;
using Microsoft.AspNetCore.Components;

namespace MudExtensions.UnitTests.Components
{
    [TestFixture]
    public class CodeInputTests : BunitTest
    {
        [TestCase("123", 3)]
        [TestCase("123456", 5)]
        public async Task CodeInput_Focus_RedirectsToFirstEmptyOrLastBox(string value, int expectedIndex)
        {
            var comp = Context.Render<MudCodeInput<string>>(opt =>
            {
                opt.Add(a => a.Count, 6);
                opt.Add(a => a.Value, value);
            });
            var fields = comp.FindComponents<MudTextFieldExtended<string>>();

            await comp.FindAll("input")[1].FocusAsync(new());

            var focused = (ElementReference)Context.JSInterop.VerifyFocusAsyncInvoke().Arguments[0]!;
            focused.Id.Should().Be(fields[expectedIndex].Instance.InputReference.ElementReference.Id);
        }
    }
}
