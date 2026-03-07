using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Config;
using Modules.Lobby.Data;
using Modules.Lobby.Exceptions;
using Modules.Lobby.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using Zenject;

namespace Modules.Lobby.Providers
{
    public class RestLobbyApiClient : ILobbyApiClient
    {
        private static readonly byte[] EmptyPayload = Array.Empty<byte>();

        private readonly ILobbyApiConfigProvider _configProvider;
        private readonly ITokenProvider _tokenProvider;

        public RestLobbyApiClient(ILobbyApiConfigProvider configProvider, ITokenProvider tokenProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
            _tokenProvider = tokenProvider;
        }

        public async UniTask<CreateGameResult> CreateGameAsync(CreateGameOptions options, CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string>
            {
                ["deck"] = options.Deck.ToString(),
                ["startingHand"] = options.StartingHand.ToString(),
                ["mode"] = string.IsNullOrWhiteSpace(options.Mode) ? "classic" : options.Mode
            };

            var uri = BuildUri("/api/games", query);
            using var request = BuildPostRequest(uri, ResolveAccessToken());
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            EnsureSuccess(request, "create game");

            var payload = request.downloadHandler.text;
            var dto = JsonConvert.DeserializeObject<CreateGameResponseDto>(payload);
            if (dto == null || string.IsNullOrWhiteSpace(dto.GameId))
            {
                throw new LobbyApiException(HttpStatusCode.OK, payload, "Lobby API returned invalid create game response.");
            }

            return new CreateGameResult(dto.GameId);
        }

        public async UniTask<JoinGameResult> JoinGameAsync(string gameId, JoinGameOptions options, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("GameId is required.", nameof(gameId));
            }

            var resolvedToken = ResolveAccessToken();
            if (string.IsNullOrWhiteSpace(resolvedToken))
            {
                throw new ArgumentException("JoinGame requires an authentication token.", nameof(options));
            }

            var uri = BuildUri($"/api/games/{gameId}/join", null);
            using var request = BuildPostRequest(uri, resolvedToken);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            EnsureSuccess(request, "join game");

            var payload = request.downloadHandler.text;
            var dto = JsonConvert.DeserializeObject<JoinGameResponseDto>(payload);
            if (dto == null || string.IsNullOrWhiteSpace(dto.PlayerId))
            {
                throw new LobbyApiException(HttpStatusCode.OK, payload, "Lobby API returned invalid join game response.");
            }

            var players = new List<LobbyPlayerInfo>();
            if (dto.Players != null)
            {
                foreach (var player in dto.Players)
                {
                    if (player == null)
                    {
                        continue;
                    }

                    var id = player.ResolveId();
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        continue;
                    }

                    players.Add(new LobbyPlayerInfo(id, false));
                }
            }

            return new JoinGameResult(
                dto.PlayerId,
                players.ToArray(),
                dto.DeckCount,
                dto.TotalCards,
                dto.Started);
        }

        public async UniTask<MatchmakingResult> SearchMatchAsync(MatchmakingOptions options, CancellationToken cancellationToken = default)
        {
            var resolvedToken = ResolveAccessToken();
            if (string.IsNullOrWhiteSpace(resolvedToken))
            {
                throw new ArgumentException("Matchmaking requires an authentication token.", nameof(options));
            }

            var uri = BuildUri("/api/matchmaking/search", null);
            var requestDto = new MatchmakingRequestDto
            {
                Deck = (int)options.Deck,
                StartingHand = options.StartingHand,
                Mode = string.IsNullOrWhiteSpace(options.Mode) ? "classic" : options.Mode
            };

            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(requestDto));
            using var request = BuildPostRequest(uri, resolvedToken, payload);
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            EnsureSuccess(request, "search match");

            var responsePayload = request.downloadHandler.text;
            var dto = JsonConvert.DeserializeObject<MatchmakingResponseDto>(responsePayload);
            if (dto == null || string.IsNullOrWhiteSpace(dto.PlayerId) || string.IsNullOrWhiteSpace(dto.GameId))
            {
                throw new LobbyApiException(HttpStatusCode.OK, responsePayload, "Lobby API returned invalid matchmaking response.");
            }

            var players = new List<LobbyPlayerInfo>();
            if (dto.Players != null)
            {
                foreach (var player in dto.Players)
                {
                    if (player == null)
                        continue;

                    var id = player.ResolveId();
                    if (string.IsNullOrWhiteSpace(id))
                        continue;

                    players.Add(new LobbyPlayerInfo(id, false));
                }
            }

            return new MatchmakingResult(
                dto.GameId,
                dto.PlayerId,
                players.ToArray(),
                dto.DeckCount,
                dto.TotalCards,
                dto.Started);
        }

        public async UniTask StartGameAsync(string gameId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("GameId is required.", nameof(gameId));
            }

            var uri = BuildUri($"/api/games/{gameId}/start", null);
            using var request = BuildPostRequest(uri, ResolveAccessToken());
            await request.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);
            EnsureSuccess(request, "start game");
        }

        private string BuildUri(string relativePath, Dictionary<string, string> query)
        {
            var config = _configProvider.GetConfig();
            var baseAddress = config.BaseAddress ?? new Uri("http://localhost");
            var builder = new UriBuilder(baseAddress);
            var trimmedBasePath = builder.Path?.TrimEnd('/') ?? string.Empty;
            var trimmedRelative = relativePath.TrimStart('/');
            builder.Path = $"{trimmedBasePath}/{trimmedRelative}";

            if (query != null && query.Count > 0)
            {
                builder.Query = string.Join("&", query
                    .Where(pair => !string.IsNullOrWhiteSpace(pair.Value))
                    .Select(pair => $"{UnityWebRequest.EscapeURL(pair.Key)}={UnityWebRequest.EscapeURL(pair.Value)}"));
            }
            else
            {
                builder.Query = string.Empty;
            }

            return builder.Uri.ToString();
        }

        private static UnityWebRequest BuildPostRequest(string uri, string accessToken, byte[] payload = null)
        {
            payload ??= EmptyPayload;
            var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(payload),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrWhiteSpace(accessToken))
            {
                request.SetRequestHeader("Authorization", $"Bearer {accessToken}");
            }
            return request;
        }

        private string ResolveAccessToken()
        {
            var token = _tokenProvider?.GetToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            return string.Empty;
        }

        private static void EnsureSuccess(UnityWebRequest request, string context)
        {
            if (request.result == UnityWebRequest.Result.Success)
            {
                return;
            }

            var statusCode = (HttpStatusCode)request.responseCode;
            var body = request.downloadHandler?.text;
            var message = $"Lobby API failed to {context}: {(int)statusCode} {statusCode} {request.error}";
            Debug.LogError($"[LobbyApi] {message}. Response: {body}");
            throw new LobbyApiException(statusCode, body, message);
        }

        private sealed class CreateGameResponseDto
        {
            [JsonProperty("gameId")]
            public string GameId { get; set; }
        }

        private sealed class JoinGameResponseDto
        {
            [JsonProperty("playerId")]
            public string PlayerId { get; set; }

            [JsonProperty("players")]
            public PlayerDto[] Players { get; set; }

            [JsonProperty("deckCount")]
            public int DeckCount { get; set; }

            [JsonProperty("totalCards")]
            public int TotalCards { get; set; }

            [JsonProperty("started")]
            public bool Started { get; set; }
        }

        private sealed class PlayerDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Id")]
            public string LegacyId { get; set; }

            public string ResolveId() => !string.IsNullOrWhiteSpace(Id) ? Id : LegacyId;

        }

        private sealed class MatchmakingRequestDto
        {
            [JsonProperty("deck")]
            public int Deck { get; set; }

            [JsonProperty("startingHand")]
            public int StartingHand { get; set; }

            [JsonProperty("mode")]
            public string Mode { get; set; }
        }

        private sealed class MatchmakingResponseDto
        {
            [JsonProperty("gameId")]
            public string GameId { get; set; }

            [JsonProperty("playerId")]
            public string PlayerId { get; set; }

            [JsonProperty("players")]
            public PlayerDto[] Players { get; set; }

            [JsonProperty("deckCount")]
            public int DeckCount { get; set; }

            [JsonProperty("totalCards")]
            public int TotalCards { get; set; }

            [JsonProperty("started")]
            public bool Started { get; set; }
        }
    }
}
