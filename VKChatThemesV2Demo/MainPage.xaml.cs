using Newtonsoft.Json;
using System;
using System.Linq;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Web.Http;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace VKChatThemesV2Demo {
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {
        public MainPage() {
            this.InitializeComponent();
            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(TitleBar);

            Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, RoutedEventArgs e) {
            Window.Current.SizeChanged += (a, b) => {
                TBRoot.Visibility = ApplicationView.GetForCurrentView().IsFullScreenMode ? Visibility.Collapsed : Visibility.Visible;
            };

            HttpRequestMessage hmsg = new HttpRequestMessage(HttpMethod.Get, new Uri("https://elorucov.github.io/laney/v2/chat_styles.json"));
            HttpClient hc = new HttpClient();
            HttpResponseMessage response = await hc.SendRequestAsync(hmsg);
            response.EnsureSuccessStatusCode();

            string json = await response.Content.ReadAsStringAsync();
            App.Styles = JsonConvert.DeserializeObject<ConversationStylesV2>(json);
            App.Styles.Styles = App.Styles.Styles.OrderBy(s => s.Sort).ToList();

            StylesCB.ItemsSource = App.Styles.Styles;
            StylesCB.SelectedIndex = 0;
        }

        private void StylesCB_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            cbc.ChatStyle = (Style)StylesCB.SelectedItem;
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e) {
            if (ApplicationView.GetForCurrentView().IsFullScreenMode) {
                ApplicationView.GetForCurrentView().ExitFullScreenMode();
            } else {
                ApplicationView.GetForCurrentView().TryEnterFullScreenMode();
            }
        }
    }
}