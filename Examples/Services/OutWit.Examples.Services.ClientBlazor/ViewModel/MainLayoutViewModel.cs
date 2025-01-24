using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Runtime;

namespace OutWit.Examples.Services.ClientBlazor.ViewModel
{
    public class MainLayoutViewModel : LayoutComponentBase
    {
        #region Constructors

        public MainLayoutViewModel()
        {
            InitDefaults();
        }

        #endregion

        #region Initialization

        private void InitDefaults()
        {
            Theme = new MudTheme();

            Title = "BLAZOR WEB SOCKET CLIENT";

            DrawerOpened = true;
        }

        #endregion

        #region Event Handlers

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        #endregion

        #region Parameters

        [Parameter]
        public MudTheme Theme { get; set; } = null!;

        [Parameter]
        public string Title { get; set; } = null!;

        [Parameter]
        public bool DrawerOpened { get; set; }

        #endregion

        #region Injections

        #endregion
    }
}
