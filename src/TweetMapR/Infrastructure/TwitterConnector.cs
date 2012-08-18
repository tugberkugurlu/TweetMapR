using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using TwitterDoodle.Data;
using TwitterDoodle.Http;
using TwitterDoodle.OAuth;

namespace TweetMapR.Infrastructure {
    
    public class TwitterConnector : IDisposable {

        private const string sampleEndpointUri = "https://stream.twitter.com/1/statuses/sample.json";
        private const string filterEndpointUri = "https://stream.twitter.com/1/statuses/filter.json";

        private string consumerKey = ConfigurationManager.AppSettings["ConsumerKey"];
        private string consumerSecret = ConfigurationManager.AppSettings["ConsumerSecret"];
        private string token = ConfigurationManager.AppSettings["Token"];
        private string tokenSecret = ConfigurationManager.AppSettings["TokenSecret"];

        private OAuthCredential _creds;
        private OAuthSignatureEntity _signatureEntity;
        private TwitterHttpClient _httpClient;

        public TwitterConnector() {

            _creds = new OAuthCredential(consumerKey) { Token = token };
            _signatureEntity = new OAuthSignatureEntity(consumerSecret) { TokenSecret = tokenSecret };
        }

        public Task<HttpResponseMessage> GetSampleFirehoseConnection() {

            if (_httpClient != null) {
                throw new NotSupportedException("Multiple connections with one instance of TwitterConnector is not allowed");
            }

            _httpClient = new TwitterHttpClient(_creds, _signatureEntity);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

            return _httpClient.GetAsync(sampleEndpointUri, HttpCompletionOption.ResponseHeadersRead);
        }

        public Task<HttpResponseMessage> GetLocationBasedConnection(string location) {

            if (_httpClient != null) {
                throw new NotSupportedException("Multiple connections with one instance of TwitterConnector is not allowed");
            }

            TwitterQueryCollection collection = new TwitterQueryCollection();
            //{southwest}long,lat,{northeast}long,lat
            //The polygon which covers the whole world
            //collection.Add("locations", "-165.0,-75.0,165.0,75.0");
            collection.Add("locations", location);

            _httpClient = new TwitterHttpClient(_creds, _signatureEntity, collection);
            _httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

            var filterContent = new StringContent(collection.ToString());
            filterContent.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            var filterRequest = new HttpRequestMessage(HttpMethod.Post, filterEndpointUri);
            filterRequest.Content = filterContent;

            return _httpClient.SendAsync(filterRequest, HttpCompletionOption.ResponseHeadersRead);
        }

        public void Dispose() {

            if (_httpClient != null) {
                _httpClient.Dispose();
            }
        }
    }
}