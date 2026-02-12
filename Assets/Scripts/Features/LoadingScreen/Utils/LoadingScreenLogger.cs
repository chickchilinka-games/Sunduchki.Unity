using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Features.LoadingScreen.Utils
{
	/// <summary>
	/// Internal logger for tracking LoadingScreen Show/Hide calls.
	/// Maintains in-memory log and persists to file in Application.persistentDataPath.
	/// </summary>
	public class LoadingScreenLogger
	{
		private readonly List<string> _callLog = new List<string>();
		private readonly string _logFilePath;
		private const int MaxLogEntries = 1000;

		public LoadingScreenLogger()
		{
			_logFilePath = Path.Combine(Application.persistentDataPath, "LoadingScreenLog.txt");
			Debug.Log($"[LoadingScreenLogger] Log file path: {_logFilePath}");
		}

		/// <summary>
		/// Logs a Show() call with timestamp.
		/// </summary>
		public void LogShow()
		{
			string logEntry = FormatLogEntry("Show()");
			AddLogEntry(logEntry);
			WriteToFile(logEntry);
		}

		/// <summary>
		/// Logs a Hide() call with timestamp.
		/// </summary>
		public void LogHide()
		{
			string logEntry = FormatLogEntry("Hide()");
			AddLogEntry(logEntry);
			WriteToFile(logEntry);
		}

		/// <summary>
		/// Gets the full in-memory log as a formatted string.
		/// </summary>
		public string GetFullLog()
		{
			if (_callLog.Count == 0)
			{
				return "[LoadingScreenLogger] No calls logged yet.";
			}

			var sb = new StringBuilder();
			sb.AppendLine($"[LoadingScreenLogger] Total calls: {_callLog.Count}");
			sb.AppendLine("----------------------------------------");
			foreach (var entry in _callLog)
			{
				sb.AppendLine(entry);
			}
			return sb.ToString();
		}

		private void AddLogEntry(string entry)
		{
			_callLog.Add(entry);

			// Prevent unbounded memory growth
			if (_callLog.Count > MaxLogEntries)
			{
				_callLog.RemoveAt(0);
			}
		}

		private void WriteToFile(string entry)
		{
			try
			{
				// Append to file
				File.AppendAllText(_logFilePath, entry + Environment.NewLine);
			}
			catch (Exception ex)
			{
				Debug.LogError($"[LoadingScreenLogger] Failed to write to log file: {ex.Message}");
			}
		}

		private string FormatLogEntry(string methodName)
		{
			string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
			return $"[{timestamp}] {methodName} called";
		}
	}
}
