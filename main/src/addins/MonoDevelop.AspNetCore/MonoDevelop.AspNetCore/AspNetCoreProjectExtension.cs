﻿//
// AspNetCoreProjectExtension.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.AspNetCore
{
	[ExportProjectModelExtension]
	class AspNetCoreProjectExtension : DotNetCoreProjectExtension
	{
		public const string TypeScriptCompile = "TypeScriptCompile";

		bool updating;
		Dictionary<string, AspNetCoreRunConfiguration> aspNetCoreRunConfs = new Dictionary<string, AspNetCoreRunConfiguration> ();
		LaunchProfileProvider launchProfileProvider;

		protected override ProjectRunConfiguration OnCreateRunConfiguration (string name)
		{
			InitLaunchSettingsProvider ();

			if (aspNetCoreRunConfs.TryGetValue (name, out var aspNetCoreRunConfiguration))
				return aspNetCoreRunConfiguration;

			var profile = new LaunchProfileData ();

			var key = name != "Default" ? name : this.Project.DefaultNamespace;

			if (!launchProfileProvider.Profiles.TryGetValue (key, out var _)) {
				profile = launchProfileProvider.AddNewProfile (key);
				launchProfileProvider.Profiles [key] = profile;
			} else {
				profile = launchProfileProvider.Profiles [key];
			}

			var aspnetconf = new AspNetCoreRunConfiguration (name, profile);
			aspnetconf.SaveRequested += Aspnetconf_Save;
			aspNetCoreRunConfs.Add (name, aspnetconf);
			return aspnetconf;
		}

		void Aspnetconf_Save (object sender, EventArgs e)
		{
			if (sender is AspNetCoreRunConfiguration config && config.IsDirty)
				launchProfileProvider.SaveLaunchSettings ();
		}

		void InitLaunchSettingsProvider ()
		{
			if (launchProfileProvider == null) {
				launchProfileProvider = new LaunchProfileProvider (this.Project.BaseDirectory, this.Project.DefaultNamespace);
				launchProfileProvider.LoadLaunchSettings ();
			}
		}

		protected override bool SupportsObject (WorkspaceObject item)
		{
			return DotNetCoreSupportsObject (item) && IsWebProject ((DotNetProject)item);
		}

		protected override bool IsSupportedFramework (TargetFrameworkMoniker framework)
		{
			return framework.IsNetCoreApp ();
		}

		protected override ExecutionCommand OnCreateExecutionCommand (
			ConfigurationSelector configSel,
			DotNetProjectConfiguration configuration,
			TargetFrameworkMoniker framework,
			ProjectRunConfiguration runConfiguration)
		{
			var result = CreateAspNetCoreExecutionCommand (configSel, configuration, runConfiguration);
			if (result != null)
				return result;

			return base.OnCreateExecutionCommand (configSel, configuration, framework, runConfiguration);
		}

		private ExecutionCommand CreateAspNetCoreExecutionCommand (ConfigurationSelector configSel, DotNetProjectConfiguration configuration, ProjectRunConfiguration runConfiguration)
		{
			FilePath outputFileName;
			if (!(runConfiguration is AspNetCoreRunConfiguration aspnetCoreRunConfiguration))
				return null;
			if (aspnetCoreRunConfiguration.StartAction == AssemblyRunConfiguration.StartActions.Program)
				outputFileName = aspnetCoreRunConfiguration.StartProgram;
			else
				outputFileName = GetOutputFileName (configuration);

			var applicationUrl = aspnetCoreRunConfiguration.CurrentProfile.TryGetApplicationUrl ();

			return new AspNetCoreExecutionCommand (
				string.IsNullOrWhiteSpace (aspnetCoreRunConfiguration.StartWorkingDirectory) ? Project.BaseDirectory : aspnetCoreRunConfiguration.StartWorkingDirectory,
				outputFileName,
				aspnetCoreRunConfiguration.StartArguments
			) {
				EnvironmentVariables = aspnetCoreRunConfiguration.EnvironmentVariables,
				PauseConsoleOutput = aspnetCoreRunConfiguration.PauseConsoleOutput,
				ExternalConsole = aspnetCoreRunConfiguration.ExternalConsole,
				LaunchBrowser = aspnetCoreRunConfiguration.CurrentProfile.LaunchBrowser ?? false,
				LaunchURL = aspnetCoreRunConfiguration.CurrentProfile.LaunchUrl,
				ApplicationURL = applicationUrl.GetFirstApplicationUrl (),
				ApplicationURLs = applicationUrl,
				PipeTransport = aspnetCoreRunConfiguration.PipeTransport
			};
		}

		protected override Task OnExecute (
			ProgressMonitor monitor,
			ExecutionContext context,
			ConfigurationSelector configuration,
			TargetFrameworkMoniker framework,
			SolutionItemRunConfiguration runConfiguration)
		{
			if (DotNetCoreRuntime.IsInstalled) {
				return CheckCertificateThenExecute (monitor, context, configuration, framework, runConfiguration);
			}
			return base.OnExecute (monitor, context, configuration, framework, runConfiguration);
		}

		protected override IEnumerable<ExecutionTarget> OnGetExecutionTargets (ConfigurationSelector configuration)
		{
			var result = new List<ExecutionTarget> ();
			foreach (var browser in IdeServices.DesktopService.GetApplications ("https://localhost")) {
				if (browser.IsDefault) {
					result.Insert (0, new AspNetCoreExecutionTarget (browser));
				} else {
					result.Add (new AspNetCoreExecutionTarget (browser));
				}
			}

			return result.Count > 0 ? result : base.OnGetExecutionTargets (configuration);
		}

		async Task CheckCertificateThenExecute (
			ProgressMonitor monitor,
			ExecutionContext context,
			ConfigurationSelector configuration,
			TargetFrameworkMoniker framework,
			SolutionItemRunConfiguration runConfiguration)
		{
			if (AspNetCoreCertificateManager.CheckDevelopmentCertificateIsTrusted (Project, runConfiguration)) {
				await AspNetCoreCertificateManager.TrustDevelopmentCertificate (monitor);
			}
			await base.OnExecute (monitor, context, configuration, framework, runConfiguration);
		}

		protected override string OnGetDefaultBuildAction (string fileName)
		{
			var mimeTypeChain = IdeServices.DesktopService.GetMimeTypeInheritanceChainForFile (fileName);
			if (mimeTypeChain.Contains ("text/x-typescript") || mimeTypeChain.Contains ("application/typescript"))
				return TypeScriptCompile;

			return base.OnGetDefaultBuildAction (fileName);
		}

		protected override void OnItemReady ()
		{
			base.OnItemReady ();
			FileService.FileChanged += FileService_FileChanged;

			InitLaunchSettingsProvider ();

			foreach (var profile in launchProfileProvider.Profiles) {

				if (profile.Value.CommandName != "Project")
					continue;

				var key = string.Empty;

				if (profile.Key == this.Project.DefaultNamespace) {
					key = "Default";
				} else {
					key = profile.Key;
				}

				var runConfig = this.Project.RunConfigurations.FirstOrDefault (x => x.Name == key);
				if (runConfig == null) {
					var projectRunConfiguration = new AspNetCoreRunConfiguration (key, profile.Value) {
						StartAction = "Project"
					};
					this.Project.RunConfigurations.Add (projectRunConfiguration);
				} else if (runConfig is AspNetCoreRunConfiguration config) {
					config.UpdateProfile (profile.Value);
				}
			}
		}

		public override void Dispose ()
		{
			base.Dispose ();
			FileService.FileChanged -= FileService_FileChanged;
			foreach (var conf in aspNetCoreRunConfs) {
				conf.Value.SaveRequested -= Aspnetconf_Save;
			}
			aspNetCoreRunConfs.Clear ();
			aspNetCoreRunConfs = null;
		}

		void FileService_FileChanged (object sender, FileEventArgs e)
		{
			var launchSettingsPath = launchProfileProvider?.LaunchSettingsJsonPath;
			var launchSettings = e.FirstOrDefault (x => x.FileName == launchSettingsPath && !x.FileName.IsDirectory);
			if (launchSettings == null)
				return;

			updating = true;

			launchProfileProvider.LoadLaunchSettings ();

			foreach (var profile in launchProfileProvider.Profiles) {
				if (profile.Value.CommandName != "Project")
					continue;

				var key = string.Empty;

				if (profile.Key == this.Project.DefaultNamespace) {
					key = "Default";
				} else {
					key = profile.Key;
				}

				var runConfig = this.Project.RunConfigurations.FirstOrDefault (x => x.Name == key);
				if (runConfig == null) {
					var projectRunConfiguration = new AspNetCoreRunConfiguration (key, profile.Value) {
						StartAction = "Project"
					};
					this.Project.RunConfigurations.Add (projectRunConfiguration);
				} else {
					if (runConfig is AspNetCoreRunConfiguration aspNetCoreRunConfiguration) {
						var index = Project.RunConfigurations.IndexOf (runConfig);
						aspNetCoreRunConfiguration.UpdateProfile (profile.Value);
						this.Project.RunConfigurations [index] = runConfig;
					}
				}
			}

			var itemsRemoved = new RunConfigurationCollection ();

			foreach (var config in Project.RunConfigurations) {
				var key = config.Name;

				if (config.Name == "Default") {
					key = this.Project.DefaultNamespace;
				}

				if (launchProfileProvider.Profiles.TryGetValue (key, out var _))
					continue;

				itemsRemoved.Add (config);
			}

			Project.RunConfigurations.RemoveRange (itemsRemoved);

			updating = false;
		}

		protected override void OnRemoveRunConfiguration (IEnumerable<SolutionItemRunConfiguration> items)
		{
			if (updating || launchProfileProvider == null)
				return;
			foreach (var item in items) {
				launchProfileProvider.Profiles.TryRemove (item.Name, out var _);
			}
			launchProfileProvider.SaveLaunchSettings ();
		}
	}
}
