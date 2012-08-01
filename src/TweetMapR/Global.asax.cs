using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SignalR;
using TweetMapR.Hubs;
using TwitterDoodle.Data;
using TwitterDoodle.Http;
using TwitterDoodle.OAuth;

namespace TweetMapR {

    public class Global : System.Web.HttpApplication {

        public readonly static ConcurrentDictionary<string, object> State = new ConcurrentDictionary<string, object>();

        protected void Application_Start(object sender, EventArgs e) {

            Task.Factory.StartNew(() => FireUpTheWork());
        }

        private void FireUpTheWork() {

            //TODO: No exception handling so far. Handle it better.
            //TODO: Don't block as much as possible and don't use Result

            var sampleEndpointUri = "https://stream.twitter.com/1/statuses/sample.json";
            var filterEndpointUri = "https://stream.twitter.com/1/statuses/filter.json";
            var consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
            var consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
            var token = ConfigurationManager.AppSettings["Token"];
            var tokenSecret = ConfigurationManager.AppSettings["TokenSecret"];
            var globalStreamGroupName = "Global";

            CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
            State["cts"] = cancellationTokenSource;

            OAuthCredential creds = new OAuthCredential(consumerKey) { Token = token };
            OAuthSignatureEntity signatureEntity = new OAuthSignatureEntity(consumerSecret) { TokenSecret = tokenSecret };

            var isCycleOn = true;

            TwitterQueryCollection collection = new TwitterQueryCollection();
            //{southwest}long,lat,{northeast}long,lat
            //The polygon which covers the whole world
            collection.Add("locations", "-165.0,-75.0,165.0,75.0");

            using (TwitterHttpClient client = new TwitterHttpClient(creds, signatureEntity, collection)) {

                client.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);
                //var response = client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead).Result;
                //var contentResult = response.Content.ReadAsStreamAsync().Result;

                var filterContent = new StringContent(collection.ToString());
                filterContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                var filterRequest = new HttpRequestMessage(HttpMethod.Post, filterEndpointUri);
                filterRequest.Content = filterContent;

                var response = client.SendAsync(filterRequest, HttpCompletionOption.ResponseHeadersRead).Result;
                var contentResult = response.Content.ReadAsStreamAsync().Result;

                using (var streamReader = new StreamReader(contentResult, Encoding.UTF8)) {
                    
                    var clients = GlobalHost.ConnectionManager.GetHubContext<TwitterHub>().Clients;

                    var cts = (CancellationTokenSource)State["cts"];
                    while (!streamReader.EndOfStream && !cts.IsCancellationRequested) {
                        
                        var result = streamReader.ReadLine();
                        if (!string.IsNullOrEmpty(result)) {

                            var tweetJToken = JsonConvert.DeserializeObject<dynamic>(result);
                            var tweetObj = tweetJToken["text"];

                            if (tweetObj != null) {

                                var tweetText = tweetObj.ToString();
                                var userScreenName = tweetJToken["user"]["screen_name"].ToString();
                                var imageUrl = tweetJToken["user"]["profile_image_url_https"].ToString();

                                var tweet = new TweetMapR.Entities.Model.Tweet() { TweetText = tweetText, User = userScreenName, ImageUrl = imageUrl };

                                var coordinatesRoot = tweetJToken["coordinates"];

                                if (coordinatesRoot != null && !string.IsNullOrEmpty(coordinatesRoot.ToString())) {

                                    var coordinates = coordinatesRoot["coordinates"];
                                    tweet.Longitude = coordinates[0];
                                    tweet.Latitude = coordinates[1];
                                }

                                //broadcast tweet to global stream subscribers
                                clients[globalStreamGroupName].broadcastTweet(tweet);
                            }
                        }
                    }
                    if (cts.IsCancellationRequested) {
                        isCycleOn = false;
                    }
                }
            }

            if (isCycleOn) {
                FireUpTheWork();
            }
        }
    }
}