using Features.LoadingScreen.Services;
using Features.LoadingScreen.Utils;
using Features.LoadingScreen.Views;
using UnityEngine;
using Zenject;

namespace Features.LoadingScreen.Bootstrap
{
	/// <summary>
	/// Zenject installer for LoadingScreen Feature.
	/// Binds ILoadingScreenService and all dependencies for global loading screen.
	/// Must be installed in ProjectContext to ensure availability across all scenes.
	/// </summary>
	public class LoadingScreenInstaller : MonoInstaller
	{
		[SerializeField]
		[Tooltip("Prefab containing LoadingScreenCanvas with BlockerView")]
		private LoadingScreenCanvas _canvasPrefab;

		private void InstallLoadingScreen()
		{
			// Instantiate and bind the canvas as a singleton
			// This will be marked as DontDestroyOnLoad in its Awake method
			var canvasInstance = Container.InstantiatePrefabForComponent<LoadingScreenCanvas>(_canvasPrefab);
			Container.Bind<LoadingScreenCanvas>()
				.FromInstance(canvasInstance)
				.AsSingle()
				.NonLazy();

			// Bind logger as singleton
			Container.Bind<LoadingScreenLogger>()
				.AsSingle();

			// Bind service by interface and concrete type
			Container.BindInterfacesAndSelfTo<LoadingScreenService>()
				.AsSingle()
				.NonLazy();

			Debug.Log("[LoadingScreenInstaller] LoadingScreen Feature installed successfully");
		}

		public override void InstallBindings()
		{
			InstallLoadingScreen();
		}
	}
}