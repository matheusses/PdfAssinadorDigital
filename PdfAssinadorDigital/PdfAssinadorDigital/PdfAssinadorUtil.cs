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

namespace PdfAssinadorDigital
{
    public static class PdfAssinadorUtil
    {
        public static byte[] AssinarPdf(X509Certificate2 certificate, byte[] sourceDocument, string colaborator, string razao, string local, bool allowInvalidCertificate)
        {
            try
            {
                // ler arquivo e insere dados de assinatura
                using (PdfReader reader = new PdfReader(sourceDocument))
                {
                    using (MemoryStream fout = new MemoryStream())
                    {
                        PdfStamper stamper = PdfStamper.CreateSignature(reader, fout, '\0');

                        // Atributos da assinatura plotada no pdf
                        PdfSignatureAppearance appearance = stamper.SignatureAppearance;
                        appearance.LocationCaption = string.Empty;
                        appearance.ReasonCaption = string.Empty;
                        appearance.Reason = razao;
                        appearance.Location = local;
                        //appearance.SignatureGraphic = Image.GetInstance("~/img/top-logo.png");
                        appearance.Layer4Text = "Documento certificado por " + colaborator;

                        Rectangle rect = new Rectangle(600, 100, 300, 150);
                        Chunk c = new Chunk();
                        rect.Chunks.Add(c);
                        appearance.SetVisibleSignature(rect, reader.NumberOfPages, "Assinatura");

                        appearance.SignatureRenderingMode = PdfSignatureAppearance.RenderingMode.DESCRIPTION;

                        ICollection<Org.BouncyCastle.X509.X509Certificate> certChain;
                        IExternalSignature es = ResolveExternalSignatureFromCertStore(certificate, allowInvalidCertificate, out certChain);

                        //Autenticação da assinatura digital
                        MakeSignature.SignDetached(appearance, es, certChain, null, null, null, 0, CryptoStandard.CMS);

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
        public static X509Certificate2 SelectCertificate()
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
}
