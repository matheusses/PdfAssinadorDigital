using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.security;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using static iTextSharp.text.Font;

namespace PdfAssinadorDigital
{
    public static class PdfAssinadorUtil
    {
        public static byte[] AssinarPdf(X509Certificate2 certificate, DadosAssinatura dadosAssinatura)
        {
            try
            {
               
                // ler arquivo e insere dados de assinatura
                using (PdfReader reader = new PdfReader(dadosAssinatura.ArquivoPdf))
                {
                    using (MemoryStream fout = new MemoryStream())
                    {
                        PdfStamper stamper = PdfStamper.CreateSignature(reader, fout, '\0');

                        // texto marca d'água
                        Font f = new Font(Font.FontFamily.UNDEFINED, 10);
                        Phrase pAssinado = new Phrase("Assinado digitalmente por:", f);
                        string[] dados = certificate.GetNameInfo(X509NameType.SimpleName, false).Split(':');

                        Phrase pNome = new Phrase(dados[0], f);
                        Phrase pDocumento = new Phrase(dados[1], f);                        
                        Phrase pData = new Phrase(certificate.GetEffectiveDateString(), f);
                        Phrase pServico = new Phrase(dadosAssinatura.Servico, f);
                        // Imagem marca d'água
                        Image img = dadosAssinatura.Imagem;
                        float w = 200F;
                        float h = 75.2F;
                        // Transparência
                        PdfGState gs1 = new PdfGState();
 
                        // Propriedades
                        PdfContentByte over;
                        Rectangle pagesize;
                                               
                        int n = reader.NumberOfPages;
                    
                        //Página
                        var pagina = 1;
                        pagesize = reader.GetPageSizeWithRotation(pagina);


                        switch (dadosAssinatura.PaginaAssinatura)
                        {
                            case EnumPaginaAssinatura.PRIMEIRA:
                                pagina = 1;
                                break;
                            case EnumPaginaAssinatura.ULTIMA:
                                pagina = reader.NumberOfPages;
                                break;
                            default:
                                pagina = 1;
                                break;
                        }
                        float x, y, xr = 0, hr = 0, yr = 0, wr = 0;
                        //Posição da assinatura
                        switch (dadosAssinatura.Posicao)
                        {
                            case EnumPosicao.ACIMA_ESQUERDA:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Top * 0.88);
                                xr = x * 0.5F;
                                wr = w;
                                yr = pagesize.Top * 0.97F;
                                hr = pagesize.Top * 0.88F;
                                
                                break;
                            case EnumPosicao.ACIMA_DIREITA:
                                x = (float)(pagesize.Right * 0.64);
                                y = (float)(pagesize.Top * 0.88);
                                xr = pagesize.Right * 0.97F;
                                wr = xr - w;
                                yr = pagesize.Top * 0.97F;
                                hr = pagesize.Top * 0.88F;
                                break;
                            case EnumPosicao.ABAIXO_ESQUERDA:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Bottom * 0.88);
                                xr = x * 0.5F;
                                wr = w;
                                yr = y;
                                hr = h;
                                break;
                            case EnumPosicao.ABAIXO_DIREITA:
                                x = (float)(pagesize.Right * 0.64);
                                y = (float)(pagesize.Bottom * 0.88);
                                xr = x * 1.53F;
                                wr = w * 1.9F;
                                yr = y;
                                hr = h;
                                break;
                            default:
                                x = (float)(pagesize.Left * 0.88);
                                y = (float)(pagesize.Top * 0.88);
                                xr = x * 1.53F;
                                wr = w * 1.9F;
                                break;
                        }

                        //Plota a assinatura no pdf
                        over = stamper.GetOverContent(pagina);
                        over.SaveState();
                        over.SetGState(gs1);
                        over.AddImage(img, w, 0, 0, h, x, y);
                        ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pAssinado, x + 10, y + 60, 0);
                        ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pNome, x + 10, y + 50, 0);
                        ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pDocumento, x + 10, y + 40, 0);
                        ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pData, x + 10, y + 25, 0);
                        ColumnText.ShowTextAligned(over, Element.ALIGN_TOP, pServico, x+ 10, y + 10, 0);
                        over.RestoreState();
                        
                        PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                        appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;
                        appearance.Layer2Text = "";
                        appearance.Layer4Text = "";
                        Rectangle rect = new Rectangle(wr ,hr,xr , yr);
                        appearance.SetVisibleSignature(rect,pagina,"Assinatura Digital");

                        ICollection<Org.BouncyCastle.X509.X509Certificate> certChain;
                        IExternalSignature es = ResolveExternalSignatureFromCertStore(certificate, dadosAssinatura.CertificadoValido, out certChain);

                        //Autenticação da assinatura digital
                        MakeSignature.SignDetached(appearance, es, certChain, null, null, null, 0, CryptoStandard.CADES);

                        stamper.Close();
                        return fout.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Erro durante assinatura digital do pdf: {0}", ex.Message);
                throw;
            }
        }
        public static X509Certificate2 SelecionarCertificado()
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            try
            {
                //Criar um armazenamento de certificado utilizando a conta de usuário.
                if (store == null)
                    throw new Exception("Não há certificado digital disponível.");
                //Abrindo o armazenamento de certificados.
                store.Open(OpenFlags.ReadOnly);

                //Selecionando um certificado
                X509CertificateCollection certificates = X509Certificate2UI.SelectFromCollection(store.Certificates, "Lista de certificados", "Por favor, selecione um certificado", X509SelectionFlag.SingleSelection);

                //Recuperação para o certificado selecionado.
                X509Certificate2 certificate = null;

                if (certificates.Count != 0)
                    certificate = (X509Certificate2)certificates[0];

                return certificate;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError("Erro durante a seleção de certificado: {0}", ex.Message);
                throw;
            }
            finally
            {
                store.Close();
            }
        }
        private static IExternalSignature ResolveExternalSignatureFromCertStore(X509Certificate2 cert, bool allowInvalidCertificate, out ICollection<Org.BouncyCastle.X509.X509Certificate> chain)
        {
            try
            {
                X509Certificate2 signatureCert = new X509Certificate2(cert);
                Org.BouncyCastle.X509.X509Certificate bcCert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(cert);
                chain = new List<Org.BouncyCastle.X509.X509Certificate> { bcCert };

                var parser = new Org.BouncyCastle.X509.X509CertificateParser();
                var bouncyCertificate = parser.ReadCertificate(cert.GetRawCertData());
                var algorithm = DigestAlgorithms.GetDigest(bouncyCertificate.SigAlgOid);
                var signature = new X509Certificate2Signature(signatureCert, algorithm);

                return signature;
            }
            catch (Exception)
            {
                throw;
            }
        }

    }
    public enum EnumPosicao
    {
        ACIMA_ESQUERDA = 1,
        ACIMA_DIREITA = 2,
        ABAIXO_ESQUERDA = 3,
        ABAIXO_DIREITA = 4
    }
    public enum EnumPaginaAssinatura
    {
        PRIMEIRA = 1,
        ULTIMA = 2
    }

    public class DadosAssinatura
    {
        public Image Imagem { get; set; }
        public string Servico { get; set; }
        public EnumPosicao Posicao { get; set; }
        public EnumPaginaAssinatura PaginaAssinatura { get; set; }
        public bool CertificadoValido { get; set; }
        public byte[] ArquivoPdf { get; set; }
    }
}
