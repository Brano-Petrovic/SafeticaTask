using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SafeticaTask.Models;

namespace SafeticaTask.Services
{
    public interface IFooterService
    {
        CommandModel CheckAndInitParams(string[] args);
        int FindIndex(byte[] sourceFileBytes);
        string GetFooter(byte[] sourceFile, int index);
        bool ProcessCommand(string footer, CommandModel paramsModel, out string newFooter);
        Task Save(byte[] fileBytes, int footerIndex, string footer, string filePath);
    }

    public class FooterService : IFooterService
    {
        private readonly string _footerPattern = "[SafeticaProperties]";

        public CommandModel CheckAndInitParams(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("ERROR: Application needs 3 arguments to start.");
                return null;
            }

            if (!File.Exists(args[1]))
            {
                Console.WriteLine($"ERROR: File '{args[1]}' do not exist!");
                return null;
            }

            var command = new CommandModel
            {
                Operator = args[0].Trim().ToLower(),
                FilePath = args[1],
                PropertyFullString = args[2],
                PropertyName = args[2].Split("=")[0]
            };

            if (args[2].Split("=").Length < 2 && command.Operator != "remove")
            {
                Console.WriteLine("ERROR: Property does not have value.");
                return null;
            }
            return command;
        }

        public string GetFooter(byte[] sourceFile, int index)
        {
            var footer = _footerPattern;
            if (index != -1)
            {
                var footerBytes = sourceFile.Skip(index).ToArray();
                footer = Encoding.ASCII.GetString(footerBytes);
            }

            return footer;
        }

        public int FindIndex(byte[] sourceFileBytes)
        {
            var footerPatternBytes = Encoding.ASCII.GetBytes(_footerPattern);

            for (int i = sourceFileBytes.Length - 1; (i >= footerPatternBytes.Length - 1 && i >= (sourceFileBytes.Length - 1024)); i--)     // footer is always on end of file, so it check file from last byte.
            {                                                                                                                               // It check last 1024 characters.
                if (sourceFileBytes[i] != footerPatternBytes[footerPatternBytes.Length - 1])                                                // compare only first byte
                    continue;

                for (int j = 1; j < footerPatternBytes.Length; j++)                                                                         // found a match on first byte, now try to match rest of the pattern
                {
                    if (sourceFileBytes[i - j] != footerPatternBytes[footerPatternBytes.Length - 1 - j]) break;
                    if (j == footerPatternBytes.Length - 1)
                        return i - (footerPatternBytes.Length - 1);
                }
            }

            return -1;
        }

        public bool ProcessCommand(string footer, CommandModel paramsModel, out string newFooter)
        {
            var existProperty = Regex.IsMatch(footer, paramsModel.PropertyName);
            newFooter = null;
            switch (paramsModel.Operator)
            {
                case "add":
                    if (existProperty)
                    {
                        Console.WriteLine("ERROR: The property exists with this name!");
                        return false;
                    }
                    else
                        newFooter = $"{footer}\n{paramsModel.PropertyFullString}";
                    break;

                case "edit":
                    if (existProperty)
                        newFooter = Regex.Replace(footer, $"{paramsModel.PropertyName}=[^\n\\n]*", paramsModel.PropertyFullString);
                    else
                    {
                        Console.WriteLine("ERROR: The property does not exist with this name!");
                        return false;
                    }
                    break;

                case "remove":
                    if (existProperty)
                        newFooter = Regex.Replace(footer, $"(?:\\n|$){paramsModel.PropertyName}=[^\n\\n]*", string.Empty);
                    else
                    {
                        Console.WriteLine("ERROR: The property does not exist with this name!");
                        return false;
                    }

                    if (newFooter.Length == _footerPattern.Length)
                        newFooter = string.Empty;
                    break;
                default:
                    Console.WriteLine("ERROR: There is invalid operation type. You can use operations: add, edit or remove.");
                    return false;
            }
            return true;
        }

        public async Task Save(byte[] fileBytes, int footerIndex, string footer, string filePath)
        {
            if (footer.Length > 1024)
            {
                Console.WriteLine("ERROR: Footer is too long. It can contains max 1024 characters!");
                return;
            }

            if (footerIndex != -1)
                fileBytes = fileBytes.Take(footerIndex).ToArray();

            fileBytes = fileBytes.Concat(Encoding.ASCII.GetBytes(footer)).ToArray();
            await File.WriteAllBytesAsync(filePath, fileBytes);
        }
    }
}
