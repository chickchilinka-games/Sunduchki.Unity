using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Cysharp.Threading.Tasks;
using Modules.Lobby.Config;
using Modules.Lobby.Data;
using Modules.Lobby.Exceptions;
using Modules.Lobby.Interfaces;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Modules.Lobby.Providers
{
    public class RestLobbyApiClient : ILobbyApiClient
    {
        private static readonly byte[] EmptyPayload = Array.Empty<byte>();

        private readonly ILobbyApiConfigProvider _configProvider;

        public RestLobbyApiClient(ILobbyApiConfigProvider configProvider)
        {
            _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
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
            using var request = BuildPostRequest(uri);
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

            if (string.IsNullOrWhiteSpace(options.Token))
            {
                throw new ArgumentException("JoinGame requires an authentication token.", nameof(options));
            }

            var query = new Dictionary<string, string>
            {
                ["token"] = options.Token
            };
            if (!string.IsNullOrWhiteSpace(options.Name))
            {
                query["name"] = options.Name;
            }

            var uri = BuildUri($"/api/games/{gameId}/join", query);
            using var request = BuildPostRequest(uri);
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

                    var name = player.ResolveName();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        name = GenerateFallbackName(id);
                    }

                    players.Add(new LobbyPlayerInfo(id, name, false));
                }
            }

            return new JoinGameResult(
                dto.PlayerId,
                string.IsNullOrWhiteSpace(dto.Name) ? GenerateFallbackName(dto.PlayerId) : dto.Name,
                players.ToArray(),
                dto.DeckCount,
                dto.TotalCards);
        }

        public async UniTask StartGameAsync(string gameId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(gameId))
            {
                throw new ArgumentException("GameId is required.", nameof(gameId));
            }

            var uri = BuildUri($"/api/games/{gameId}/start", null);
            using var request = BuildPostRequest(uri);
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

        private static UnityWebRequest BuildPostRequest(string uri)
        {
            var request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(EmptyPayload),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/json");
            return request;
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

        private static string GenerateFallbackName(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId))
            {
                return "Player";
            }

            var suffix = playerId.Length > 4 ? playerId[..4] : playerId;
            return $"Player {suffix}";
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

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("players")]
            public PlayerDto[] Players { get; set; }

            [JsonProperty("deckCount")]
            public int DeckCount { get; set; }

            [JsonProperty("totalCards")]
            public int TotalCards { get; set; }
        }

        private sealed class PlayerDto
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("Id")]
            public string LegacyId { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("Name")]
            public string LegacyName { get; set; }

            public string ResolveId() => !string.IsNullOrWhiteSpace(Id) ? Id : LegacyId;

            public string ResolveName() => !string.IsNullOrWhiteSpace(Name) ? Name : LegacyName;
        }
    }
}
