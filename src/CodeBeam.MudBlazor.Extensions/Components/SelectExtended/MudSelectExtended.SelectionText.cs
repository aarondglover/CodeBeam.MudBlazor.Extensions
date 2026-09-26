namespace MudExtensions
{
    public partial class MudSelectExtended<T>
    {
        /// <inheritdoc />
        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            // SelectedValues is a synchronous parameter setter. Its existing async text update
            // cannot be awaited there, so reconcile collection-backed presentation after the
            // complete parameter set has been applied. This is value-driven and does not depend
            // on any MudSelectItemExtended component being materialized.
            if (MultiSelection && ItemCollection is not null)
                await UpdateTextPropertyAsync(false);
        }
    }
}
