using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.Versioning;
using System;

namespace GamePathFinder
{
    [SupportedOSPlatform("windows")]
    class SteamGamePath
    {
        public static string? GetSteamPath => ((string?)Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null))?.Replace("/", "\\\\");
        // Paths in Windows are case insensitive, so I don't bother making sure SteamLibraryPaths and this are the same case.
        public static List<string>? SteamLibraryPaths()
        {
            var libraryfoldersPath = new StringBuilder(GetSteamPath);
            if (libraryfoldersPath == null) // Steam not installed or Registry key is missing
                return null;
            var toReturn = new List<string>(); // We can have as little as 1 or up to an unknown amount of paths. 
            libraryfoldersPath.Append("\\steamapps\\libraryfolders.vdf"); // This file holds all paths
            //var unsortedPaths = "";
            var unsortedPaths = new StringBuilder();
            using (var sr = File.OpenText(libraryfoldersPath.ToString()))
            {
                string? currentLine;
                while ((currentLine = sr.ReadLine()) != null)
                    if (currentLine.Contains("path"))
                        unsortedPaths.Append(currentLine);
            }
            string[] strings = SplitByQuotes(unsortedPaths.ToString());
            for (var i = 1; i < strings.Length; i += 2) // We only need the paths, not the library number
            {
                toReturn.Add(strings[i].Trim('\"')); // Get rid of the quotes
            }
            return toReturn;
        }
        static string? FindGameACFByAppID(string appID)
        {
            /* If we do not add this other games with the same number in 
             * its name can be returned instead.
             * ex - Try 730 (cs:go) and have 976730 (MCC) installed.
             * If we find MCC first we return that instead of csgo.
             * All appID in steam file start with _ and ends with .acf.
             */
            appID = $"_{appID}.acf";
            var paths = SteamLibraryPaths();
            if (paths == null)
                return null;
            foreach (var path in paths)
            {
                var steamappPath = $"{path}\\steamapps\\"; // ACF files are in steamapp folder
                var filesInPath = Directory.GetFiles(steamappPath);
                foreach (var file in filesInPath)
                    if (file.Contains(appID)) // If we find the appID we can stop looking
                        return file;
            }
            return null; // Game not installed or something is wrong.
        }
        // I split these up so it looks neater.
        public static string? FindGameByAppID(string appID)
        {
            var ACFFile = FindGameACFByAppID(appID); // ACF file has the install folder
            if (ACFFile == null)
                return null;
            using (var sr = File.OpenText(ACFFile))
            {
                string? currentLine;
                while ((currentLine = sr.ReadLine()) != null)
                    if (currentLine.Contains("installdir"))
                    {
                        string[] currentLineArr = SplitByQuotes(currentLine);
                        /* Instead of refinding the whole file path again I just remove the .acf file
                         * and add on common and the installdir.
                         */
                        //return ACFFile.Substring(0, ACFFile.LastIndexOf("\\") + 1) + "common\\" + currentLineArr[1].Trim('\"') /*+ "\\"*/;
                        return string.Concat(ACFFile.AsSpan(0, ACFFile.LastIndexOf("\\") + 1), $"common\\{currentLineArr[1].Trim('\"')}");
                    }
            }
            return null;
        }
        public static string? FindGameByAppID(UInt64 appID)
        {
            return FindGameByAppID(appID.ToString());
        }
        static string[] SplitByQuotes(string unsplitArray)
        {
            var re = new Regex("\"[^\"]*\"");
            return re.Matches(unsplitArray).Cast<Match>().Select(m => m.Value).ToArray(); // Split into an array using quotes to split
        }
    }
    [SupportedOSPlatform("windows")]
    class GoGGamePath
    {
        public static string? FindGameByAppID(string appID)
        {
            return (string?)Registry.GetValue($"HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\GOG.com\\Games\\{appID}\\", "Path", null);
        }
    }
}
