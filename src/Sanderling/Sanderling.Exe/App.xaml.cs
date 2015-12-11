﻿using Bib3;
using BotEngine.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace Sanderling.Exe
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		static public Int64 GetTimeStopwatch() => Bib3.Glob.StopwatchZaitMiliSictInt();

		public const string ConfigApiVersionDefaultAddress = @"http://sanderling.api.botengine.de:4034/api";

		BotEngine.LicenseClientConfig LicenseClientConfig => ConfigReadFromUI()?.LicenseClient;

		public MainWindow Window => base.MainWindow as MainWindow;

		Bib3.FCL.GBS.ToggleButtonHorizBinär ToggleButtonMotionEnable => Window?.Main?.ToggleButtonMotionEnable;

		BotScript.UI.Wpf.IDE ScriptIDE => Window?.Main?.Bot?.IDE;

		UI.BotAPIExplorer BotAPIExplorer => Window?.Main?.Bot?.APIExplorer;

		BotScript.ScriptRun ScriptRun => ScriptIDE?.ScriptRun;

		bool WasActivated = false;

		DispatcherTimer Timer;

		string AssemblyDirectoryPath => Bib3.FCL.Glob.ZuProcessSelbsctMainModuleDirectoryPfaadBerecne().PathToFilesysChild(@"\");

		Sanderling.Script.HostToScript UIAPI;

		public App()
		{
			AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;

			UIAPI = new Sanderling.Script.HostToScript()
			{
				MemoryMeasurementFunc = new Func<FromProcessMeasurement<Interface.MemoryMeasurementEvaluation>>(() => MemoryMeasurementLast),
			};
		}

		private System.Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var MatchFullName =
				AppDomain.CurrentDomain.GetAssemblies()
				?.FirstOrDefault(candidate => string.Equals(candidate.GetName().FullName, args?.Name));

			if (null != MatchFullName)
			{
				return MatchFullName;
			}

			var MatchName =
				AppDomain.CurrentDomain.GetAssemblies()
				?.FirstOrDefault(candidate => string.Equals(candidate.GetName().Name, args?.Name));

			return MatchName;
		}

		void TimerConstruct()
		{
			Timer = new DispatcherTimer(TimeSpan.FromSeconds(1.0 / 4), DispatcherPriority.Normal, Timer_Tick, Dispatcher);

			Timer.Start();
		}

		private void Application_Activated(object sender, EventArgs e)
		{
			if (WasActivated)
			{
				return;
			}

			WasActivated = true;

			ActivatedFirstTime();
		}

		Script.ToScriptGlobals ToScriptGlobalsConstruct(Action ScriptExecutionCheck) =>
			new Script.ToScriptGlobals()
			{
				Sanderling = new Sanderling.Script.HostToScript()
				{
					MemoryMeasurementFunc = () =>
					{
						ScriptExecutionCheck?.Invoke();
						return FromScriptRequestMemoryMeasurementEvaluation();
					},

					MotionExecuteFunc = MotionParam =>
					{
						ScriptExecutionCheck?.Invoke();
						return FromScriptMotionExecute(MotionParam);
					},
				}
			};

		void ActivatedFirstTime()
		{
			ScriptIDE.ScriptRunGlobalsFunc = ToScriptGlobalsConstruct;

			ScriptIDE.ScriptParamBase = new BotScript.ScriptParam()
			{
				ImportAssembly = Script.ToScriptImport.ImportAssembly?.ToArray(),
				ImportNamespace = Sanderling.Script.ToScriptImport.ImportNamespace?.ToArray(),
				CompilationOption = new BotScript.CodeAnalysis.CompilationOption()
				{
					InstrumentationOption = BotScript.CodeAnalysis.Default.InstrumentationOption,
				},
			};

			ScriptIDE.ChooseScriptFromIncludedScripts.SetScript =
				ListScriptIncluded?.Select(ScriptIdAndContent => new KeyValuePair<string, Func<string>>(ScriptIdAndContent.Key, () => ScriptIdAndContent.Value))?.ToArray();

			ScriptIDE.ScriptWriteToOrReadFromFile.DefaultFilePath = DefaultScriptPath;
			ScriptIDE.ScriptWriteToOrReadFromFile?.ReadFromFile();
			ScriptIDE.Editor.Document.Text = ScriptIDE.Editor.Document.Text ?? ListScriptIncluded?.FirstOrDefault().Value ?? "";

			Window.KeyDown += Window_KeyDown;
			Window?.AddHandler(System.Windows.Controls.Primitives.ButtonBase.ClickEvent, new RoutedEventHandler(ButtonClicked));

			Window?.Main?.ConfigFromModelToView(ConfigDefaultConstruct());

			ConfigFileControl.DefaultFilePath = ConfigFilePath;
			ConfigFileControl.CallbackGetValueToWrite = ConfigReadFromUISerialized;
			ConfigFileControl.CallbackValueRead = ConfigWriteToUIDeSerialized;
			ConfigFileControl.ReadFromFile();

			TimerConstruct();
		}

		void Timer_Tick(object sender, object e)
		{
			ProcessInput();

			Motor = GetMotor();

			ScriptExchange();

			LicenseClientExchange();

			UIPresent();
		}

		void ButtonClicked(object sender, RoutedEventArgs e)
		{
			var OriginalSource = e?.OriginalSource;

			if (null != OriginalSource)
			{
				if (OriginalSource == ToggleButtonMotionEnable?.ButtonLinx)
				{
					ScriptRunPause();
				}

				if (OriginalSource == ToggleButtonMotionEnable?.ButtonRecz)
				{
					ScriptRunPlay();
				}
			}
		}

		void ScriptRunPlay()
		{
			ScriptIDE.ScriptRunContinueOrStart();

			ScriptExchange();
		}

		void ScriptRunPause()
		{
			ScriptIDE.ScriptPause();

			ScriptExchange();
		}

		void ScriptExchange()
		{
			ToggleButtonMotionEnable.ButtonReczIsChecked = ScriptRun?.IsRunning ?? false;

			ScriptIDE?.Present();
		}

	}
}
