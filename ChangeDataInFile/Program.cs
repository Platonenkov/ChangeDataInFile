using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChangeDataInFile
{
    class Program
    {
        private static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        private static Dictionary<string, int> FileChanges = new();

        //private static IEnumerable<Section> Sections = new List<Section>(
        //new[]
        //{
        //    new Section()
        //    {
        //        FileMask = "*.csproj", Updating = new[]
        //        {
        //            new UpdatingData(){ Old = "RRJ-Express.ContainerCore, Version=1.1.1.5", New = "RRJ-Express.ContainerCore, Version=1.1.1.6"},
        //            new UpdatingData(){ Old = "RRJ-Express.ContainerCore.1.1.1.5", New = "RRJ-Express.ContainerCore.1.1.1.6"},
        //            new UpdatingData(){ Old = "RRJ-Express.ExpressCore, Version=1.0.0.23", New = "RRJ-Express.ExpressCore, Version=1.0.0.24"},
        //            new UpdatingData(){ Old = "RRJ-Express.ExpressCore.1.0.0.23", New = "RRJ-Express.ExpressCore.1.0.0.24"}
        //        }
        //    },
        //    new Section()
        //    {
        //        FileMask = "packages.config", Updating = new []
        //        {
        //            new UpdatingData(){ Old = "RRJ-Express.ContainerCore\" version=\"1.1.1.5", New = "RRJ-Express.ContainerCore\" version=\"1.1.1.6" },
        //            new UpdatingData(){ Old = "RRJ-Express.ExpressCore\" version=\"1.0.0.23", New="RRJ-Express.ContainerCore\" version=\"1.0.0.24" },
        //        }
        //    }
        //});

        private const string SettingFileName = "UpdateFilesScheme.json";
        static async Task Main(string[] args)
        {
            //await JsonInFile.SaveToFileAsync(SettingFileName, Sections);
            Console.WriteLine("Read configuration file");
            if(!File.Exists(SettingFileName))
            {
                "Configuration file not found".ConsoleRed();
                try
                {
                    "Attempt to create a configuration file".ConsoleYellow();
                    await JsonInFile.SaveToFileAsync(SettingFileName, 
                        new Section[]
                        {
                            new Section()
                                {
                                    FileMask = "InputMask - *.csproj or file name",
                                    Updating = new []
                                    {
                                        new UpdatingData()
                                        {
                                            Old = "OldValue",New = "NewValue"
                                        },
                                        new UpdatingData()
                                        {
                                            Old = "OldValue",New = "NewValue"
                                        }
                                    }
                                }
                        });
                    $"Please enter configuration to the file - {SettingFileName}".PrintMessgeAndWaitEnter();
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    Console.ReadLine();
                    return;
                }
            }
            IEnumerable<Section> sections;
            try
            {
                var data = await JsonInFile.LoadFromFile<IEnumerable<Section>>(SettingFileName);
                if (data is null)
                {
                    "Configuration is not correct".ConsoleRed();
                    "press any Enter to close programm".PrintMessgeAndWaitEnter();
                    return;
                }

                sections = data;
            }
            catch (Exception e)
            {
                $"Error to read configuration - {e.Message}".ConsoleRed();
                Console.WriteLine(e);
                "press any Enter to close programm".PrintMessgeAndWaitEnter();
                return;
            }
            var currDir = CurrentDirectory;

            var watcher = new Stopwatch();
            watcher.Start();

            TakeFilesFromProjects(currDir, sections);
            Console.WriteLine($"Completed in {GetStringTime(watcher.Elapsed)}");
            lock (FileChanges)
            {
                if (FileChanges.Count == 0)
                {
                    "No changes".ConsoleRed();
                }
                else
                    $"{FileChanges.Count} file(s) was changed".ConsoleYellow();

            }
            "press any Enter to close programm".PrintMessgeAndWaitEnter();
        }

        private static void TakeFilesFromProjects(DirectoryInfo directory, IEnumerable<Section> sections)
        {
            if (!directory.Exists)
            {
                "No Directory".ConsoleRed();
                return;
            }
            Console.WriteLine($"Find package files...");

            var tasks = new List<Task>();
            foreach (var section in sections)
            {
                var files = directory.EnumerateFiles(section.FileMask, SearchOption.AllDirectories).ToArray();
                if (files.Length == 0)
                {
                    $"No files in mask ".ConsoleYellow();
                    section.FileMask.ConsoleRed();
                    continue;
                }

                var end = files.Length > 1 ? "s" : "";
                $"Found {files.Length} file{end} with mask - {section.FileMask}".ConsoleGreen();

                var updating = section.Updating.ToArray();
                foreach (var file in files)
                {
                    tasks.Add(UpdatePackageInFileAsync2(file.FullName, updating));
                }
            }
            if(tasks.Count == 0)
                return;

            //foreach (var task in tasks)
            //{
            //    task.Start();
            //}

            Task.WaitAll(tasks.ToArray());
        }

        private static async Task<bool> UpdatePackageInFileAsync2(string filePath, UpdatingData[] Updating)
        {
            try
            {
                var changes = 0;
                var text = await File.ReadAllTextAsync(filePath);
                text = Updating.Aggregate(text, (current, package) =>
                {
                    if (current.Contains(package.Old))
                        changes++;
                    return current.Replace(package.Old, package.New);
                });
                if (changes > 0)
                    lock (FileChanges)
                    {
                        FileChanges.Add(filePath, changes);
                        $"File - {filePath}\nChange - {changes} row(s)".ConsoleYellow();
                    }
                else
                    return false;
                await File.WriteAllTextAsync(filePath, text);
                return true;
            }
            catch (Exception e)
            {
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return false;
            }
        }

        private static async Task<bool> UpdatePackageInFileAsync(string filePath, UpdatingData[] Updating)
        {
            try
            {
                var new_file_name = Path.ChangeExtension(filePath, "packageTemp");
                File.Copy(filePath, new_file_name, true);

                using (var streame = new StreamReader(filePath))
                {
                    await using var sw = new StreamWriter(new_file_name, false);
                    while (!streame.EndOfStream)
                    {
                        if (await streame.ReadLineAsync() is {Length: > 0} text)
                        {
                            var flag = false;
                            foreach (var (old_data, new_data) in Updating)
                                if (text.Contains(old_data))
                                {
                                    await sw.WriteLineAsync(text.Replace(old_data, new_data));
                                    flag = true;
                                    break;
                                }
                            if(!flag)
                                await sw.WriteLineAsync(text);
                        }
                        else
                            await sw.WriteLineAsync();
                    }
                }
                //File.Delete(filePath);
                //File.Replace(new_file_name, Path.ChangeExtension(filePath, Path.GetExtension(filePath)), $"C:\\Temp\\UptatePackages\\{Path.GetFileName(filePath)}");
                //File.Replace(filePath, new_file_name, $"C:\\Temp\\UptatePackages\\{Path.GetFileName(filePath)}");
                File.Move(new_file_name,filePath,true);
                return true;
            }
            catch (Exception e)
            {
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return false;
            }

        }
        private static async Task<bool> UpdatePackageInFileAsync(string filePath, UpdatingData Updating)
        {
            try
            {
                var new_file_name = Path.ChangeExtension(filePath, "packageTemp");
                File.Copy(filePath, new_file_name, true);

                using var streame = new StreamReader(filePath);
                await using var sw = new StreamWriter(new_file_name, false);
                while (!streame.EndOfStream)
                {
                    if (await streame.ReadLineAsync() is { Length: > 0 } text)
                    {
                        if (text.Contains(Updating.Old))
                        {
                            await sw.WriteLineAsync(text.Replace(Updating.Old, Updating.New));
                        }
                        else
                            await sw.WriteLineAsync(text);
                    }
                    else
                        await sw.WriteLineAsync();
                }

                File.Replace(filePath, new_file_name, $"C:\\Temp\\UptatePackages\\{Path.GetFileName(filePath)}");
                return true;
            }
            catch (Exception e)
            {
                $"File - {filePath}\n{e.Message}\n".ConsoleRed();
                return false;
            }
        }
        /// <summary>
        /// Get time in string format
        /// </summary>
        private static string GetStringTime(TimeSpan time)
        {
            return time.Days > 0 ? time.ToString(@"d\.hh\:mm\:ss") :
                time.Hours > 0 ? time.ToString(@"hh\:mm\:ss") :
                time.Minutes > 0 ? time.ToString(@"mm\:ss") :
                time.Seconds > 0 ? time.ToString(@"g"): $"{Math.Round(time.TotalMilliseconds,0)} ms";
        }
    }
}
