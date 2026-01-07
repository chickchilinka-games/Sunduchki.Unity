using System;
using Features.UI.Components;
using Modules.Lobby.Data;
using Modules.Lobby.Services;
using UnityEngine;
using Zenject;

namespace Features.AppLifecycle.States.Home.View
{
    public class CreateGameButton: AbstractButton
    {
        private LobbyService _lobbyService;

        [Inject]
        public void Construct(LobbyService lobbyService)
        {
            _lobbyService = lobbyService;
        }
        
        protected override async void OnClick()
        {
            try
            {
                var game = await _lobbyService.CreateGameAsync(new CreateGameOptions(DeckType.Short36, 4, "classic"));
                await _lobbyService.JoinGameAsync(game.GameId, new JoinGameOptions());
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}