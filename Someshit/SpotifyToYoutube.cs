using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using SpotifyAPI.Web;
using System.Text.RegularExpressions;

namespace SpotifyToYoutube4Net
{
    public class SpotifyToYoutube
    {

        private readonly SpotifyClient _spotify;
        private readonly YouTubeService _youtube;

        /// <summary>
		/// Module constructor with:
        /// </summary>
        /// <param name="youtubeApiKey">YoutubeAPIKey, see <see href="https://developers.google.com/api-client-library/dotnet/get_started?hl=ru">how to get</see></param>
        /// <param name="clientId"></param>
        /// <param name="clientSecret"></param>
        public SpotifyToYoutube(string youtubeApiKey, string clientId, string clientSecret)
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithAuthenticator(new ClientCredentialsAuthenticator(clientId, clientSecret));
            _spotify = new SpotifyClient(config);
            _youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeApiKey,
                ApplicationName = this.GetType().ToString()
            });
        }

        /// <summary>
        /// Module constructor with:
        /// </summary>
        /// <param name="youtubeApiKey">YoutubeAPIKey, see <see href="https://developers.google.com/api-client-library/dotnet/get_started?hl=ru">how to get</see></param>
        /// <param name="spotifyToken"></param>
        public SpotifyToYoutube(string youtubeApiKey, string spotifyToken)
        {
            var config = SpotifyClientConfig
                .CreateDefault()
                .WithToken(spotifyToken);
            _spotify = new SpotifyClient(config);
            _youtube = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = youtubeApiKey,
                ApplicationName = this.GetType().ToString()
            });

        }
        /// <summary>
        /// Convert your spotify url to youtube url!
        /// </summary>
        /// <param name="url">Spotify url to convert</param
        /// <returns>Youtube url</returns>
        public string Convert(string url)
        {
            string trackId = "";

            if (!Regex.IsMatch(url, @"^((?:https?:\/\/)?open\.spotify\.com\/track\/)?([\p{L}+:/\d.]+)(\?si\=[\p{L}+:/\d.]+)?"))
            {
                throw new Exception("This is not a Spotify link");
            }
            Match mathes = Regex.Match(url, @"^((?:https?:\/\/)?open\.spotify\.com\/track\/)?([\p{L}+:/\d.]+)(\?si\=[\p{L}+:/\d.]+)?");
            trackId = mathes.Groups[2].Value;

            var track = _spotify.Tracks.Get(trackId).Result;

            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            return $"{track.Artists[0].Name} {track.Name}";

            /*var searchListRequest = _youtube.Search.List("snippet");
            searchListRequest.MaxResults = 10;
            searchListRequest.Type = "video";
            searchListRequest.VideoCategoryId = "10";
            searchListRequest.Q = $"{track.Artists[0].Name} {track.Name}";
            SearchListResponse searchListResponse = searchListRequest.Execute();

            Console.WriteLine($"Track {searchListResponse.Items[1].Snippet.Title}");

            return "https://www.youtube.com/watch?v=" + searchListResponse.Items[0].Id.VideoId;*/
        }
    }
}