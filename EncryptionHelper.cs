using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

// ============================================================
//  Şifreleme Yardımcı Sınıfı
//  Algoritma: AES-256-CBC
//
//  Kaynak: Microsoft Docs
//  https://docs.microsoft.com/tr-tr/dotnet/api/system.security.cryptography.aes
//
//  Hocam bu kısımda simetrik şifreleme kullandım.
//  Parola doğrudan key olarak kullanılmıyor, PBKDF2 ile
//  önce güvenli bir anahtar türetiliyor. Bu sayede zayıf
//  parolalara karşı da bir miktar koruma sağlanıyor.
// ============================================================

namespace SteganografiApp
{
    public class EncryptionHelper
    {
        // AES-256 için 256 bit = 32 byte anahtar gerekiyor
        private const int ANAHTAR_BOYUTU_BYTE = 32;

        // PBKDF2 iterasyon sayısı - ne kadar çok o kadar güvenli ama yavaş
        // 10000 makul bir değer (RFC 2898 en az 1000 öneriyor)
        private const int ITERASYON_SAYISI = 10000;

        // mesajı AES-256 ile şifrele, base64 string döndür
        public static string Sifrele(string duzMetin, string parola)
        {
            // her şifrelemede farklı rastgele salt ve IV üret
            // böylece aynı mesaj + aynı parola = farklı şifreli çıktı
            byte[] salt = RastgeleByte(16);
            byte[] iv   = RastgeleByte(16);

            byte[] anahtar = ParoladanAnahtar(parola, salt);

            using (Aes aes = Aes.Create())
            {
                aes.Key     = anahtar;
                aes.IV      = iv;
                aes.Mode    = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (MemoryStream ms = new MemoryStream())
                {
                    // şifreli verinin başına salt ve IV'yi yaz
                    // şifre çözerken bunlara ihtiyaç var
                    ms.Write(salt, 0, salt.Length);  // 0-15. byte: salt
                    ms.Write(iv,   0, iv.Length);    // 16-31. byte: IV

                    // şifreli veriyi yaz
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    {
                        byte[] duzMetinBytes = Encoding.UTF8.GetBytes(duzMetin);
                        cs.Write(duzMetinBytes, 0, duzMetinBytes.Length);
                        cs.FlushFinalBlock();
                    }

                    // binary veriyi base64'e çevir, string olarak saklayabilelim
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        // şifreli metni çöz
        public static string SifreCoz(string sifreliBas64, string parola)
        {
            byte[] tumVeri;

            try
            {
                tumVeri = Convert.FromBase64String(sifreliBas64);
            }
            catch
            {
                throw new Exception("Geçersiz şifreli veri formatı!");
            }

            // en az 32 byte olmalı (16 salt + 16 IV)
            if (tumVeri.Length < 32)
                throw new Exception("Veri bozuk görünüyor.");

            // salt ve IV'yi ayır (ilk 32 byte)
            byte[] salt = new byte[16];
            byte[] iv   = new byte[16];
            Array.Copy(tumVeri, 0,  salt, 0, 16);
            Array.Copy(tumVeri, 16, iv,   0, 16);

            byte[] anahtar = ParoladanAnahtar(parola, salt);

            try
            {
                using (Aes aes = Aes.Create())
                {
                    aes.Key     = anahtar;
                    aes.IV      = iv;
                    aes.Mode    = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    // 32. byte'tan itibaren gerçek şifreli veri başlıyor
                    int sifreliBoyut = tumVeri.Length - 32;
                    using (MemoryStream ms = new MemoryStream(tumVeri, 32, sifreliBoyut))
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read))
                    using (StreamReader sr = new StreamReader(cs, Encoding.UTF8))
                    {
                        return sr.ReadToEnd();
                    }
                }
            }
            catch
            {
                // şifre yanlışsa padding hatası fırlatır, biz daha anlaşılır mesaj ver
                throw new Exception("Şifre çözülemedi! Parola yanlış olabilir ya da veri bozuk.");
            }
        }

        // PBKDF2 ile paroladan güvenli AES anahtarı türet
        // direkt parola kullanmak yerine bu şekilde yapıyoruz
        private static byte[] ParoladanAnahtar(string parola, byte[] salt)
        {
            // Rfc2898DeriveBytes = PBKDF2 implementasyonu
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                parola, salt, ITERASYON_SAYISI, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(ANAHTAR_BOYUTU_BYTE);
            }
        }

        // kriptografik olarak güvenli rastgele byte dizisi üret
        private static byte[] RastgeleByte(int adet)
        {
            byte[] dizi = new byte[adet];
            RandomNumberGenerator.Fill(dizi);
            return dizi;
        }
    }
}
