using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace PandaCppCLI
{
    public class PandaCppCLIProfile
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string NamespaceIncGuard { get; set; }
        public string CreateRootPath { get; set; }
    }

    public class PandaCppCLISettings
    {
        public string DefaultProfile { get; set; }
        public Dictionary<string, PandaCppCLIProfile> Profiles { get; set; }
    }

    public enum eCommandType
    {
        Help,
        SetProfile,
        NewClass,
    }

    class Program
    {
        public static PandaCppCLISettings Settings { get; set; } = null;
        public static PandaCppCLIProfile CurrentProfile { get; set; } = null;

        public static string CurrentExeDir { get; set; } = "";

        static void Main(string[] args)
        {
            CurrentExeDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\";

            var settingsJsonString = File.ReadAllText(CurrentExeDir + "settings.json");
            Settings = JsonConvert.DeserializeObject<PandaCppCLISettings>(settingsJsonString);

            if (!string.IsNullOrWhiteSpace(Settings.DefaultProfile))
            {
                Console.WriteLine("Attempting to set default profile.");
                SetProfile(Settings.DefaultProfile);
            }

            var workingDir = Environment.CurrentDirectory;
            var commandType = eCommandType.Help;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-setprofile":
                    case "-sp":
                        {
                            commandType = eCommandType.SetProfile;
                        }
                        break;

                    case "-newclass":
                    case "-nc":
                        {
                            commandType = eCommandType.NewClass;
                        }
                        break;
                }
            }
            
            switch (commandType)
            {
                case eCommandType.SetProfile:
                    {
                        SetProfile(args);
                    }
                    break;

                case eCommandType.NewClass:
                    {
                        NewClass(args, workingDir);
                    }
                    break;

                case eCommandType.Help:
                default:
                    PrintHelp();
                    break;
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("Panda CPP CLI Commands:");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-setprofile, -sp: Set your profile to one in the settings.json.");
            Console.WriteLine("\tArguments: -p[Profile name]");
            Console.Write(Environment.NewLine);
            Console.WriteLine("-newclass, -nc: Create a new class from a standard template.");
            Console.WriteLine("\tArguments: -c[Class name] -h[Header path] -cpp[Cpp path]");
            Console.WriteLine("\tNotes: If just the header path is specified both files will go there.");
        }

        static void SetProfile(string[] args)
        {
            var profileQuery = args.Where(a => a.StartsWith("-p"));

            SetProfile(profileQuery.First().Substring(2));
        }

        static void SetProfile(string profileName)
        {
            if (!Settings.Profiles.ContainsKey(profileName))
                Console.WriteLine("Error: No profile exists by that name.");

            CurrentProfile = Settings.Profiles[profileName];
            Console.WriteLine("Profile set to: " + profileName);
        }

        static void NewClass(string[] args, string workingDir)
        {
            if (CurrentProfile == null)
                Console.WriteLine("Error: No profile set.");

            var profileQuery = args.Where(a => a.StartsWith("-p"));
            var classNameQuery = args.Where(a => a.StartsWith("-c"));
            var headerPathQuery = args.Where(a => a.StartsWith("-h"));
            var cppPathQuery = args.Where(a => a.StartsWith("-cpp"));

            if (profileQuery.Count() > 0)
                SetProfile(profileQuery.First().Substring(2));

            var className = classNameQuery.First().Substring(2);
            var headerPath = CurrentProfile.CreateRootPath + (headerPathQuery.Count() == 0 ? "" : headerPathQuery.First().Substring(2));
            var cppPath = CurrentProfile.CreateRootPath + (cppPathQuery.Count() == 0 ? headerPath : cppPathQuery.First().Substring(4));

            Console.WriteLine("Creating class:" + className);

            var fileClassName = className.ToLowercaseNamingConvention();
            var fileHeaderName = fileClassName + ".h";
            var fileCppName = fileClassName + ".cpp";
            var fullHeaderPath = headerPath + (headerPath.Length > 0 ? (headerPath.EndsWith("/") ? "" : "/") : "") + fileHeaderName;
            var fullCppPath = headerPath + (cppPath.Length > 0 ? (cppPath.EndsWith("/") ? "" : "/") : "") + fileCppName;

            var headerTemplate = ProcessCppTemplate(CurrentExeDir + "Templates/CPP/Header.txt", className, fileHeaderName);
            var cppTemplate = ProcessCppTemplate(CurrentExeDir + "Templates/CPP/CPP.txt", className, fileHeaderName);

            Directory.CreateDirectory(Path.GetDirectoryName(fullHeaderPath));
            File.WriteAllText(fullHeaderPath, headerTemplate);
            Console.WriteLine(fullHeaderPath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullCppPath));
            File.WriteAllText(fullCppPath, cppTemplate);
            Console.WriteLine(fullCppPath);
        }

        static string ProcessCppTemplate(string templatePath, string className, string includeHeader)
        {
            var template = File.ReadAllText(templatePath);
            template = template.Replace("{NAMESPACE}", CurrentProfile.Namespace);
            template = template.Replace("{NAMESPACEINCGUARD}", CurrentProfile.NamespaceIncGuard);
            template = template.Replace("{CLASSNAME}", className);
            template = template.Replace("{CLASSNAMEINCGUARD}", className.ToLowercaseNamingConvention());
            template = template.Replace("{INCLUDEHEADER}", includeHeader);

            return template;
        }
    }
}
