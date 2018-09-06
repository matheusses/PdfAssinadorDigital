using PdfAssinadorDigital;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PdfAssinadorDigital
{
    class Console
    {
        enum ExitCode : int
        {
            Success = 0,
            InvalidParameters = 1,
            FileNotFound = 2,
            OtherError = 4
        }

        static int Main(string[] args)
        {
            bool backup = true;
            string pdfFile = @"caminho do arquivo .pdf a ser assinado";
            if (!File.Exists(pdfFile))
            {
                System.Console.WriteLine("File '{0}' not found.", pdfFile);
                return (int)ExitCode.FileNotFound;
            }

            try
            {
                var fileContent = File.ReadAllBytes(pdfFile);
                X509Certificate2 certificate = PdfAssinadorUtil.SelectCertificate();
                var signedFileContent = PdfAssinadorUtil.AssinarPdf(certificate, fileContent, "usuário","razao","local", true);

                if (backup)
                    File.Move(pdfFile, pdfFile + ".bkp");

                File.WriteAllBytes(pdfFile, signedFileContent);

                return (int)ExitCode.Success;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine(ex.Message);
                return (int)ExitCode.OtherError;
            }
        }
    }
}
