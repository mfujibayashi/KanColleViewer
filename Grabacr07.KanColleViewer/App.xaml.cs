﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Grabacr07.KanColleViewer.Composition;
using Grabacr07.KanColleViewer.Models;
using Grabacr07.KanColleViewer.ViewModels;
using Grabacr07.KanColleViewer.Views;
using Grabacr07.KanColleWrapper;
using Livet;
using MetroRadiance;
using AppSettings = Grabacr07.KanColleViewer.Properties.Settings;
using Settings = Grabacr07.KanColleViewer.Models.Settings;

namespace Grabacr07.KanColleViewer
{
	public partial class App
	{
		public static ProductInfo ProductInfo { get; private set; }
		public static MainWindowViewModel ViewModelRoot { get; private set; }

		static App()
		{
			AppDomain.CurrentDomain.UnhandledException += (sender, args) => ReportException(sender, args.ExceptionObject as Exception);
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			this.DispatcherUnhandledException += (sender, args) => ReportException(sender, args.Exception);

			DispatcherHelper.UIDispatcher = this.Dispatcher;
			ProductInfo = new ProductInfo();

			Settings.Load();
			ResourceService.Current.ChangeCulture(Settings.Current.Culture);

			var initResult = PluginHost.Instance.Initialize();
			if (initResult == PluginHost.InitializationResult.RequiresRestart)
			{
				Restart(e.Args.ToString(" "));

				this.Shutdown(0);
				return;
			}
			if (initResult == PluginHost.InitializationResult.Failed)
			{
				// メッセージはリソース化するのと、「プラグイン取り除いてみろ」的なヒントを出したい感じ
				MessageBox.Show("プラグインが原因で、アプリケーションの起動に失敗しました。", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

				this.Shutdown(0);
				return;
			}

			NotifierHost.Instance.Initialize(KanColleClient.Current);
			Helper.SetRegistryFeatureBrowserEmulation();
			Helper.SetMMCSSTask();

			KanColleClient.Current.Proxy.Startup(AppSettings.Default.LocalProxyPort);
			KanColleClient.Current.Proxy.UpstreamProxySettings = Settings.Current.ProxySettings;

			ThemeService.Current.Initialize(this, Theme.Dark, Accent.Purple);

			ViewModelRoot = new MainWindowViewModel();
			this.MainWindow = new MainWindow { DataContext = ViewModelRoot };
			this.MainWindow.Show();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			base.OnExit(e);

			KanColleClient.Current.Proxy.Shutdown();

			PluginHost.Instance.Dispose();
			Settings.Current.Save();
		}

		private static void Restart(string args)
		{
			if (ProductInfo.IsDebug)
			{
				Process.Start("KanColleViewer.exe", args);
			}
			else
			{
				try
				{
					Process.Start(Environment.GetCommandLineArgs()[0], args);
				}
				catch (Exception)
				{
					MessageBox.Show("プラグインの読み込みに失敗しました。再度アプリケーションを起動してみてください。", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
		}

		private static void ReportException(object sender, Exception exception)
		{
			#region const
			const string messageFormat = @"
===========================================================
ERROR, date = {0}, sender = {1},
{2}
";
			const string path = "error.log";
			#endregion

			try
			{
				var message = string.Format(messageFormat, DateTimeOffset.Now, sender, exception);

				Debug.WriteLine(message);
				File.AppendAllText(path, message);
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}
