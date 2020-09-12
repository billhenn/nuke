﻿// Copyright 2019 Maintainers of NUKE.
// Distributed under the MIT License.
// https://github.com/nuke-build/nuke/blob/master/LICENSE

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Nuke.Common.CI;
using Nuke.Common.IO;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Constants;

namespace Nuke.Common
{
    public abstract partial class NukeBuild
    {
        static NukeBuild()
        {
            RootDirectory = GetRootDirectory();
            TemporaryDirectory = GetTemporaryDirectory(RootDirectory);
            FileSystemTasks.EnsureExistingDirectory(TemporaryDirectory);
            BuildAssemblyDirectory = GetBuildAssemblyDirectory();
            BuildProjectFile = GetBuildProjectFile(BuildAssemblyDirectory);
            BuildProjectDirectory = BuildProjectFile?.Parent;

            Verbosity = EnvironmentInfo.GetParameter<Verbosity?>(() => Verbosity) ?? Verbosity.Normal;
            Host = EnvironmentInfo.GetParameter(() => Host) ?? Host.Default;
            Continue = EnvironmentInfo.GetParameter(() => Continue);
            Plan = EnvironmentInfo.GetParameter(() => Plan);
            Help = EnvironmentInfo.GetParameter(() => Help);
            NoLogo = EnvironmentInfo.GetParameter(() => NoLogo);

            // LoadedProfiles = BuildProfileManagementAttributeBase.GetLoadProfiles();
            // SaveProfile = BuildProfileManagementAttributeBase.GetSaveProfile();
        }

        /// <summary>
        /// Gets the full path to the root directory.
        /// </summary>
        [Parameter("Root directory during build execution.", Name = "Root")]
        public static AbsolutePath RootDirectory { get; }

        /// <summary>
        /// Gets the full path to the temporary directory <c>/.tmp</c>.
        /// </summary>
        public static AbsolutePath TemporaryDirectory { get; }

        /// <summary>
        /// Gets the full path to the build assembly directory, or <c>null</c>.
        /// </summary>
        [CanBeNull]
        public static AbsolutePath BuildAssemblyDirectory { get; }

        /// <summary>
        /// Gets the full path to the build project directory, or <c>null</c>
        /// </summary>
        [CanBeNull]
        public static AbsolutePath BuildProjectDirectory { get; }

        /// <summary>
        /// Gets the full path to the build project file, or <c>null</c>
        /// </summary>
        [CanBeNull]
        public static AbsolutePath BuildProjectFile { get; }

        /// <summary>
        /// Gets the logging verbosity during build execution. Default is <see cref="Nuke.Common.Verbosity.Normal"/>.
        /// </summary>
        [Parameter("Logging verbosity during build execution. Default is 'Normal'.")]
        public static Verbosity Verbosity
        {
            get => (Verbosity) LogLevel;
            set => LogLevel = (LogLevel) value;
        }

        /// <summary>
        /// Gets the host for execution. Default is <em>automatic</em>.
        /// </summary>
        [Parameter("Host for execution. Default is 'automatic'.")]
        public static Host Host { get; }

        /// <summary>
        /// Gets a value whether to show the execution plan (HTML).
        /// </summary>
        [Parameter("Shows the execution plan (HTML).")]
        public static bool Plan { get; }

        /// <summary>
        /// Gets a value whether to show the help text for this build assembly.
        /// </summary>
        [Parameter("Shows the help text for this build assembly.")]
        public static bool Help { get; }

        /// <summary>
        /// Gets a value whether to display the NUKE logo.
        /// </summary>
        [Parameter("Disables displaying the NUKE logo.")]
        public static bool NoLogo { get; set; }

        // [Parameter("Defines the profiles to load.", Name = "LoadProfile")]
        // public static string[] LoadedProfiles { get; }
        //
        // [Parameter("Defines the profile to save to.")]
        // public static string SaveProfile { get; }

        public static bool IsLocalBuild => !IsServerBuild;
        public static bool IsServerBuild => Host is IBuildServer;

        public static LogLevel LogLevel
        {
            get => Logger.LogLevel;
            set => Logger.LogLevel = value;
        }

        [Parameter("Indicates to continue a previously failed build attempt.")]
        public static bool Continue { get; internal set; }

        private static AbsolutePath GetRootDirectory()
        {
            var parameterValue = EnvironmentInfo.GetParameter(() => RootDirectory);
            if (parameterValue != null)
                return parameterValue;

            if (EnvironmentInfo.GetParameter<bool>(() => RootDirectory))
                return EnvironmentInfo.WorkingDirectory;

            return TryGetRootDirectoryFrom(EnvironmentInfo.WorkingDirectory)
                .NotNull(new[]
                         {
                             $"Could not locate '{ConfigurationFileName}' file while walking up from '{EnvironmentInfo.WorkingDirectory}'.",
                             "Either create the file to mark the root directory, or use the --root parameter."
                         }.JoinNewLine());
        }

        [CanBeNull]
        private static AbsolutePath GetBuildAssemblyDirectory()
        {
            var entryAssembly = Assembly.GetEntryAssembly();
            if (entryAssembly == null || entryAssembly.GetTypes().All(x => !x.IsSubclassOf(typeof(NukeBuild))))
                return null;

            return (AbsolutePath) Path.GetDirectoryName(entryAssembly.Location).NotNull();
        }

        [CanBeNull]
        private static AbsolutePath GetBuildProjectFile([CanBeNull] AbsolutePath buildAssemblyDirectory)
        {
            if (buildAssemblyDirectory == null)
                return null;

            return (AbsolutePath) new DirectoryInfo(buildAssemblyDirectory)
                .DescendantsAndSelf(x => x.Parent)
                .Select(x => x.GetFiles("*.csproj", SearchOption.TopDirectoryOnly)
                    .SingleOrDefaultOrError($"Found multiple project files in '{x}'."))
                .FirstOrDefault(x => x != null)
                ?.FullName;
        }
    }
}
