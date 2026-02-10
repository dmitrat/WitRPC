using Microsoft.JSInterop;

namespace OutWit.Communication.Client.Blazor.Tests.Mocks
{
    internal sealed class TestJSRuntime : IJSRuntime
    {
        #region IJSRuntime

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return new ValueTask<TValue>(default(TValue)!);
        }

        #endregion
    }
}
