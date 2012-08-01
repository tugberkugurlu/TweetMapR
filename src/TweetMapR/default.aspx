<%@ Page Language="C#" %>
<!DOCTYPE html>
<html>
<head>
    <title>TweetMapR - SignalR + Twitter Streaming API + Google Map === This is looove...</title>
    <link rel="stylesheet" type="text/css" href="/Content/bootstrap.min.css" />
    <link rel="stylesheet" type="text/css" href="/Content/bootstrap-responsive.min.css" />
    <link rel="stylesheet" type="text/css" href="/Content/toastr.css" />
    <link rel="stylesheet" type="text/css" href="/Content/toastr-responsive.css" />
    <link rel="stylesheet" type="text/css" href="/Content/Style.css" />
    <% if (ConfigurationManager.AppSettings["GoogleAnalytics:ID"] != null && !string.IsNullOrEmpty(ConfigurationManager.AppSettings["GoogleAnalytics:ID"].ToString())) { %>
    <script type="text/javascript">
        var _gaq = _gaq || [];
        _gaq.push(['_setAccount', '<%: ConfigurationManager.AppSettings["GoogleAnalytics:ID"] %>']);
        _gaq.push(['_trackPageview']);
        (function () {
            var ga = document.createElement('script'); ga.type = 'text/javascript'; ga.async = true;
            ga.src = ('https:' == document.location.protocol ? 'https://ssl' : 'http://www') + '.google-analytics.com/ga.js';
            var s = document.getElementsByTagName('script')[0]; s.parentNode.insertBefore(ga, s);
        })();
    </script>
    <% } %>
</head>
<body>
    <a class="forkme" href="https://github.com/tugberkugurlu/TweetMapR"></a>
    <div class="navbar navbar-fixed-top">
        <div class="navbar-inner">
            <div class="container">
                <a class="brand" href="/">TweetMapR</a>
            </div>
        </div>
    </div>

    <div id="main" class="container">
        
        <div id="connection-control-panel" class="row">
            <div class="span12">
                <button id="startBtn" class="btn btn-small" type="button"><i class="icon-play"></i> Start</button>
                <button id="stopBtn" class="btn btn-small" type="button"><i class="icon-stop"></i> Stop</button>
                <div class="pull-right"></div>
            </div>
        </div>

        <div class="row">
            <div id="map-holder" class="span8">
                <div id="map_canvas"></div>
            </div>
            <div id="messages-holder" class="span4">
                <ul id="messages"></ul>
            </div>
        </div>
    </div>

    <script type="text/javascript" src="/Scripts/jquery-1.7.2.js"></script>
    <script type="text/javascript" src="/Scripts/jquery.signalR-0.5.2.js"></script>
    <script type="text/javascript" src="/SignalR/Hubs"></script>
    <script type="text/javascript" src="/Scripts/knockout-2.1.0.js"></script>
    <script type="text/javascript" src="/Scripts/toastr.js"></script>
    <script src="https://maps.googleapis.com/maps/api/js?sensor=false"></script>
    <script>
        //TODO: Seperate the js from HTML
        //TODO: Seperate the concerns as well. Create events and trigger them when appropriate

        $(function () {

            var twitterHub = $.connection.twitterHub,
                $startBtn = $("#startBtn"),
                $stopBtn = $("#stopBtn"),
                map;

            toastr.options.fadeOut = 1000;
            toastr.options.timeOut = 1500;
            //toastr.options.positionClass = "toast-top-center";

            function calculateToastrPosition() {

                var windowWidth = $(window).width(),
                    toastMsgWidth = 300;

                var leftOffset = (windowWidth - toastMsgWidth) / 2;
                $('<style>').text(".toast-top-center { top: 10px; left: " + leftOffset + "px; }").appendTo("head");
            }

            function initialize() {

                var mapOptions = {
                    zoom: 2,
                    center: new google.maps.LatLng(34.397, -10.644),
                    mapTypeId: google.maps.MapTypeId.HYBRID
                };
                map = new google.maps.Map(document.getElementById("map_canvas"), mapOptions);
            }

            function subscribeGlobalStream() {

                twitterHub.subscribeToStreamGroup("Global").done(function (result) {

                    toastr.success("Successfully subscribed to Global Twitter stream.");
                    if (result === true) {

                        $startBtn.prop("disabled", true);
                        $stopBtn.prop("disabled", false);
                    }
                });
            }

            function unsubscribeGlobalStream() {

                twitterHub.unsubscribeFromStreamGroup("Global").done(function () {

                    //TODO: Display friendly notification messages about the status
                    toastr.success("Successfully unsubscribed from Global Twitter stream.");
                    $stopBtn.prop("disabled", true);
                    $startBtn.prop("disabled", false);
                });
            }

            initialize();

            calculateToastrPosition();

            twitterHub.broadcastTweet = function (tweet) {

                $("#messages").prepend(
                    $("<li>").html('<span style="color: green;">Tweet by <strong>@' + tweet.User + '</strong>:</span> ' + tweet.TweetText)
                );

                if (tweet.Longitude) {

                    var marker = new google.maps.Marker({
                        animation: google.maps.Animation.DROP,
                        position: new google.maps.LatLng(tweet.Latitude, tweet.Longitude),
                        title: tweet.TweetText + " by @" + tweet.User
                    });

                    var infowindow = new google.maps.InfoWindow({
                        content: '<img style="height:40px;margin-right:10px;" src="' + tweet.ImageUrl + '" /><span style="color: green;">Tweet by <strong>@' + tweet.User + '</strong>:</span> ' + tweet.TweetText
                    });

                    marker.setMap(map);

                    google.maps.event.addListener(marker, 'click', function () {
                        infowindow.open(map, marker);
                    });
                }
            }

            $.connection.hub.start().done(function () {

                subscribeGlobalStream();

                $stopBtn.click(function (e) {

                    unsubscribeGlobalStream();
                    e.preventDefault();
                });

                $startBtn.click(function (e) {

                    subscribeGlobalStream();
                    e.preventDefault();
                });
            });
        });
    </script>
    <!--Mode: <%: ConfigurationManager.AppSettings["TweetMapR:Mode"] %>-->
</body>
</html>