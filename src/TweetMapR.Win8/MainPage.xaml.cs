using Bing.Maps;
using Microsoft.AspNet.SignalR.Client.Hubs;
using System;
using System.Threading.Tasks;
using TweetMapR.Win8.Models;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace TweetMapR.Win8 {

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page {

        public MainPage() {

            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.  The Parameter
        /// property is typically used to configure the page.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e) {

            BingMap.Center = new Location(34.397, -10.644);
            BingMap.ZoomLevel = 2.5;

            await ConnectToTweetMaprAsync();
        }

        private async Task ConnectToTweetMaprAsync() {

            string url = "http://localhost:18066";
            HubConnection conn = new HubConnection(url);
            IHubProxy tweetsHub = conn.CreateHubProxy("TwitterHub");
            tweetsHub.On<Tweet>("broadcastTweet", ProcessTweet);
            await conn.Start();
            await tweetsHub.Invoke("SubscribeToStreamGroup", "Global");
        }

        private async void ProcessTweet(Tweet tweet) {

            if (!string.IsNullOrEmpty(tweet.Latitude) &&
                !string.IsNullOrEmpty(tweet.Longitude)) {

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {

                    Pushpin pushpin = new Pushpin();
                    pushpin.Tapped +=
                        async (s, e) => {

                            MessageDialog dialog = new MessageDialog(
                                tweet.TweetText, 
                                string.Concat("Tweet by ", tweet.User));

                            await dialog.ShowAsync();
                        };

                    MapLayer.SetPosition(pushpin, 
                        new Location(
                            double.Parse(tweet.Latitude), 
                            double.Parse(tweet.Longitude)));

                    BingMap.Children.Add(pushpin);
                });
            }
        }
    }
}