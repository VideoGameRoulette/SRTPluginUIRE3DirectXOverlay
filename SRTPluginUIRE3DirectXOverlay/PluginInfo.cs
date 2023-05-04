using SRTPluginBase;
using System;

namespace SRTPluginUIRE3DirectXOverlay
{
    internal class PluginInfo : IPluginInfo
    {
        public string Name => "DirectX Overlay UI (Resident Evil 3 (2020))";

        public string Description => "A DirectX-based Overlay User Interface for displaying Resident Evil 3 (2020) game memory values.";

        public string Author => "VideoGameRoulette";

        public Uri MoreInfoURL => new Uri("https://github.com/VideoGameRoulette/SRTPluginUIRE3DirectXOverlay");

        public int VersionMajor => assemblyFileVersion.ProductMajorPart;

        public int VersionMinor => assemblyFileVersion.ProductMinorPart;

        public int VersionBuild => assemblyFileVersion.ProductBuildPart;

        public int VersionRevision => assemblyFileVersion.ProductPrivatePart;

        private System.Diagnostics.FileVersionInfo assemblyFileVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
    }
}
