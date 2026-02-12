using Features.LoadingScreen.Utils;
using Features.LoadingScreen.Views;
using UnityEngine;

namespace Features.LoadingScreen.Services
{
	/// <summary>
	/// Service implementation for controlling the global loading screen.
	/// Coordinates between LoadingScreenCanvas (view) and LoadingScreenLogger (logging).
	/// </summary>
	public class LoadingScreenService
	{
		private readonly LoadingScreenCanvas _canvas;
		private readonly LoadingScreenLogger _logger;

		public LoadingScreenService(LoadingScreenCanvas canvas, LoadingScreenLogger logger)
		{
			_canvas = canvas;
			_logger = logger;

			Debug.Log("[LoadingScreenService] Initialized");
		}

		public void Show()
		{
			Debug.Log("[LoadingScreenService] Show() called");
			_logger.LogShow();
			_canvas.Show();
		}

		public void Hide()
		{
			Debug.Log("[LoadingScreenService] Hide() called");
			_logger.LogHide();
			_canvas.Hide();
		}

		public string GetCallLog()
		{
			return _logger.GetFullLog();
		}
	}
}
