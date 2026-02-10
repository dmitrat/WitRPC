using Microsoft.AspNetCore.Components;

namespace OutWit.Communication.Client.Blazor.Tests.Mocks
{
    internal sealed class TestNavigationManager : NavigationManager
    {
        #region Constructors

        public TestNavigationManager(string baseUri = "https://localhost:5001/")
        {
            Initialize(baseUri, baseUri);
        }

        #endregion
    }
}
