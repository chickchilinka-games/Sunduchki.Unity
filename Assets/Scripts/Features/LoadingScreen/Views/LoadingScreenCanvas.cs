
using Features.UI.Components;
using UnityEngine;

namespace Features.LoadingScreen.Views
{
	/// <summary>
	/// MonoBehaviour component managing the loading screen canvas.
	/// Contains BlockerView and persists across scenes via DontDestroyOnLoad.
	/// </summary>
	public class LoadingScreenCanvas : MonoBehaviour
	{
		[SerializeField] private Canvas _canvas;
		[SerializeField] private BlockerView _blockerView;

		private void Awake()
		{
			// Ensure this canvas persists across all scenes
			DontDestroyOnLoad(gameObject);

			// Validate references
			if (_canvas == null)
			{
				Debug.LogError("[LoadingScreenCanvas] Canvas reference is missing!");
			}

			if (_blockerView == null)
			{
				Debug.LogError("[LoadingScreenCanvas] BlockerView reference is missing!");
			}

			// Start hidden
			Hide();

			Debug.Log("[LoadingScreenCanvas] Initialized and set to DontDestroyOnLoad");
		}

		/// <summary>
		/// Shows the loading screen by enabling the canvas GameObject.
		/// </summary>
		public void Show()
		{
			if (_canvas != null)
			{
				_canvas.gameObject.SetActive(true);
				Debug.Log("[LoadingScreenCanvas] Canvas activated");
			}
		}

		/// <summary>
		/// Hides the loading screen by disabling the canvas GameObject.
		/// </summary>
		public void Hide()
		{
			if (_canvas != null)
			{
				_canvas.gameObject.SetActive(false);
				Debug.Log("[LoadingScreenCanvas] Canvas deactivated");
			}
		}
	}
}