namespace Features.LoadingScreen.Services
{
	/// <summary>
	/// Public interface for controlling the global loading screen.
	/// This service provides methods to show/hide the loading screen and access call logs.
	/// </summary>
	public interface ILoadingScreenService
	{
		/// <summary>
		/// Shows the loading screen overlay.
		/// Logs the call with timestamp to the internal log file.
		/// </summary>
		void Show();

		/// <summary>
		/// Hides the loading screen overlay.
		/// Logs the call with timestamp to the internal log file.
		/// </summary>
		void Hide();

		/// <summary>
		/// Retrieves the full call log containing all Show/Hide invocations with timestamps.
		/// </summary>
		/// <returns>Formatted string with all logged calls</returns>
		string GetCallLog();
	}
}