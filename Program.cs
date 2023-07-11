using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SafeticaTask.Services;

namespace SafeticaTask
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var _footerService = new FooterService();

            var paramsModel = _footerService.CheckAndInitParams(args);
            if (paramsModel == null)
                return;

            Console.WriteLine("Load file.");
            var fileBytes = await File.ReadAllBytesAsync(paramsModel.FilePath, CancellationToken.None);

            Console.WriteLine("Check if exist footer.");
            var index = _footerService.FindIndex(fileBytes);

            var footer = _footerService.GetFooter(fileBytes, index);

            Console.WriteLine("Process command started.");
            var isValid = _footerService.ProcessCommand(footer, paramsModel, out footer);
            if (isValid)
            {
                Console.WriteLine("Saving of file.");
                await _footerService.Save(fileBytes, index, footer, paramsModel.FilePath);
                Console.WriteLine("Footer was successful changed.");
            }
            else
            {
                Console.WriteLine("Footer was not changed.");
            }
        }
    }
}
