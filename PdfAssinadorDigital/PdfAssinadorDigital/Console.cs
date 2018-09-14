using iTextSharp.text;
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
            string pdfFile = @"";//File source
            if (!File.Exists(pdfFile))
            {
                System.Console.WriteLine("File '{0}' not found.", pdfFile);
                return (int)ExitCode.FileNotFound;
            }

            try
            {
                var fileContent = File.ReadAllBytes(pdfFile);
                var dadosAssinatura = new DadosAssinatura
                {
                    Imagem = Image.GetInstance("imagem"),
                    PaginaAssinatura = EnumPaginaAssinatura.PRIMEIRA,
                    Posicao = EnumPosicao.ABAIXO_DIREITA,
                    CertificadoValido = true,
                    ArquivoPdf = fileContent
                };
                
                X509Certificate2 certificate = PdfAssinadorUtil.SelecionarCertificado();
                var signedFileContent = PdfAssinadorUtil.AssinarPdf(certificate, dadosAssinatura);

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
