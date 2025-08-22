using System.IO;
using System.Net.Sockets;
using System.Text;
using Microsoft.Maui.Storage;

namespace LlamaPromptingApp
{
    public partial class MainPage : ContentPage
    {
        private const string PROXY_URL_STORE_KEY = "proxy_url", PROXY_PORT_STORE_KEY = "proxy_port";
        private static string _proxyUrl = "0.tcp.jp.ngrok.io";
        private static int _proxyPort = 10588;

        private PromptResponseSystem _promptResponseSystem;
            
        public MainPage()
        {
            InitializeComponent();
            this.Title = "Prompting app";
            this._promptResponseSystem = new PromptResponseSystem();
            _proxyUrl = Preferences.Get(PROXY_URL_STORE_KEY, _proxyUrl);
            _proxyPort = Preferences.Get(PROXY_PORT_STORE_KEY, _proxyPort); 
            this.entryUrl.Text = $"{_proxyUrl}:{_proxyPort}";
            this._promptResponseSystem.OnConnectionStatusUpdate = (msg, ms) =>
            {
                this.lblConnectionInfo.Text = $"Connection status : {msg}{Environment.NewLine}Latency : {ms.TotalMilliseconds} ms";
            };
        }

        private async void OnSendPromptClick(object sender, EventArgs e)
        {
            this.btnSendPrompt.IsEnabled = false;
            string prompt = this.entryPrompt.Text;
            var rsp = await this._promptResponseSystem.SendPromptAsync(prompt,_proxyUrl,_proxyPort);
            this.lblResponse.Text = rsp;
            this.btnSendPrompt.IsEnabled = true;
        }

        private void OnSaveProxyUrlClick(object sender , EventArgs e)
        {
            this.btnSaveUrl.IsEnabled = false;

            string[] urlSplit = this.entryUrl.Text.Split(":");
            string urlSegment = urlSplit.First() , portString = urlSplit.Last();

            if(int.TryParse(portString,out var portInt))
            {
                _proxyUrl  = urlSegment;
                _proxyPort = portInt;
                Preferences.Set(PROXY_URL_STORE_KEY,_proxyUrl);
                Preferences.Set(PROXY_PORT_STORE_KEY, _proxyPort);
            }
      
            this.btnSaveUrl.IsEnabled = true;
        }

        private async void OnViewChatLogClick(object sender , EventArgs e)
        {
            await Navigation.PushAsync(new LogPage(this._promptResponseSystem));
        }

    }
}
