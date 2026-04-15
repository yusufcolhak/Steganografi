using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

// ============================================================
//  Steganografi Uygulaması - Veri Gizleme Motoru
//  Yöntem: LSB (Least Significant Bit / En Az Anlamlı Bit)
//
//  Hocam bu sınıfta piksellerin RGB değerlerinin son bitini
//  değiştirerek veri gizliyoruz. Gözle fark edilemiyor çünkü
//  renk değerinde sadece 1 birim değişiyor (255 üzerinden).
//
//  Kaynak: https://en.wikipedia.org/wiki/Steganography
//          + Bilgisayar Ağları ders notları (Hafta 11)
// ============================================================

namespace SteganografiApp
{
    public class SteganographyEngine
    {
        // mesajın nerede bittiğini anlamak için özel bir işaret
        // bunu resimden okurken bu işareti görünce duruyoruz
        private const string BITIS_ISARETI = "##MESAJ_BITTI##";

        // resmin kaç byte veri sığdırabileceğini hesapla
        // her piksel R, G, B olmak üzere 3 kanal var
        // her kanalın sadece 1 bitini kullanıyoruz (LSB)
        // yani toplam kapasite = genişlik * yükseklik * 3 / 8 byte
        public static int KapasiteHesapla(Bitmap resim)
        {
            int toplamBit = resim.Width * resim.Height * 3;
            int kapasite = toplamBit / 8;
            return kapasite;
        }

        // mesajı resme göm ve sonuç resmi döndür
        public static Bitmap MesajGom(Bitmap kaynakResim, string mesaj)
        {
            // mesajın sonuna bitiş işareti ekle
            string tamMesaj = mesaj + BITIS_ISARETI;
            byte[] mesajBytes = Encoding.UTF8.GetBytes(tamMesaj);
            int toplamBit = mesajBytes.Length * 8;

            // kapasite kontrolü - mesaj sığmıyor mu?
            int maxKapasite = KapasiteHesapla(kaynakResim);
            if (mesajBytes.Length > maxKapasite)
            {
                // hata fırlat
                throw new Exception(
                    "Mesaj çok uzun! Bu resme en fazla " + maxKapasite +
                    " byte sığar. Mesajınız: " + mesajBytes.Length + " byte.");
            }

            // orijinal resmi kopyala, kopyası üzerinde çalış
            // böylece orijinal bozulmuyor
            Bitmap sonucResim = new Bitmap(kaynakResim);

            int bitSayaci = 0;

            // tüm pikselleri sırayla gez (sol üstten başla)
            for (int y = 0; y < sonucResim.Height; y++)
            {
                for (int x = 0; x < sonucResim.Width; x++)
                {
                    // tüm bitleri gömdüysek dur
                    // iç içe döngüden çıkmak için goto kullandım
                    // hocam biliyorum kötü pratik ama C#'ta başka yolu yok
                    if (bitSayaci >= toplamBit)
                        goto GOMME_TAMAMLANDI;

                    Color piksel = sonucResim.GetPixel(x, y);

                    int r = piksel.R;
                    int g = piksel.G;
                    int b = piksel.B;

                    // R kanalının LSB'sine bir bit göm
                    if (bitSayaci < toplamBit)
                    {
                        int bit = BitGetir(mesajBytes, bitSayaci);
                        r = LSBYaz(r, bit);
                        bitSayaci++;
                    }

                    // G kanalının LSB'sine bir bit göm
                    if (bitSayaci < toplamBit)
                    {
                        int bit = BitGetir(mesajBytes, bitSayaci);
                        g = LSBYaz(g, bit);
                        bitSayaci++;
                    }

                    // B kanalının LSB'sine bir bit göm
                    if (bitSayaci < toplamBit)
                    {
                        int bit = BitGetir(mesajBytes, bitSayaci);
                        b = LSBYaz(b, bit);
                        bitSayaci++;
                    }

                    // değiştirilmiş pikseli geri yaz
                    sonucResim.SetPixel(x, y, Color.FromArgb(piksel.A, r, g, b));
                }
            }

        GOMME_TAMAMLANDI:
            return sonucResim;
        }

        // resimden gizlenmiş mesajı çıkar
        public static string MesajCikar(Bitmap stegoResim)
        {
            List<byte> okunanBytes = new List<byte>();

            int bitSayaci = 0;
            byte mevcutByte = 0;

            for (int y = 0; y < stegoResim.Height; y++)
            {
                for (int x = 0; x < stegoResim.Width; x++)
                {
                    Color piksel = stegoResim.GetPixel(x, y);

                    // üç kanalı bir dizide tut, temiz görünsün diye
                    int[] kanallar = new int[] { piksel.R, piksel.G, piksel.B };

                    foreach (int kanal in kanallar)
                    {
                        // kanalın en sağ bitini al (LSB)
                        int lsb = kanal & 1;

                        // biti mevcut byte'a ekle (sola kaydır + ekle)
                        mevcutByte = (byte)((mevcutByte << 1) | lsb);
                        bitSayaci++;

                        // 8 bit doldu mu? o zaman bir byte tamamlandı
                        if (bitSayaci == 8)
                        {
                            // Gömme işlemi MSB önce (BitGetir) yapıldığı için ve okuma da << 1 yapıldığı için
                            // ters çevirmeye gerek kalmadan orijinal byte doğrudan elde edilmiştir.
                            okunanBytes.Add(mevcutByte);

                            mevcutByte = 0;
                            bitSayaci = 0;

                            // Her byte eklendiğinde bitiş işaretini kontrol et
                            // Performans için sadece son byte'ları (bitiş işaretinin boyutu kadar) kontrol ediyoruz
                            int isaretByteUzunluk = Encoding.UTF8.GetByteCount(BITIS_ISARETI);
                            if (okunanBytes.Count >= isaretByteUzunluk)
                            {
                                int baslangic = okunanBytes.Count - isaretByteUzunluk;
                                string sonYazilanlar = Encoding.UTF8.GetString(okunanBytes.ToArray(), baslangic, isaretByteUzunluk);

                                if (sonYazilanlar == BITIS_ISARETI)
                                {
                                    // Bitiş işaretini bulduk! Tamamını string'e çevir ve bitiş işaretini at.
                                    string tumMesaj = Encoding.UTF8.GetString(okunanBytes.ToArray());
                                    return tumMesaj.Substring(0, tumMesaj.Length - BITIS_ISARETI.Length);
                                }
                            }

                            // çok fazla byte okuduk ve hâlâ bulamadık = mesaj yok
                            if (okunanBytes.Count > 500000)
                            {
                                throw new Exception("Bu resimde gizli mesaj bulunamadı.");
                            }
                        }
                    }
                }
            }

            // resmin sonuna geldik ama bitiş işareti yoktu
            throw new Exception("Bu resimde gizli mesaj bulunamadı!");
        }

        // ------- yardımcı private metodlar -------

        // verili indeksteki biti döndür (MSB önce)
        private static int BitGetir(byte[] veri, int bitIndex)
        {
            int byteIndex = bitIndex / 8;
            int bitPozisyon = 7 - (bitIndex % 8);  // MSB'den başlıyoruz
            return (veri[byteIndex] >> bitPozisyon) & 1;
        }

        // bir sayının en sağ bitini (LSB) istediğimiz değere ata
        private static int LSBYaz(int deger, int bit)
        {
            // önce LSB'yi sıfırla (& 0xFE = 11111110)
            // sonra yeni biti OR ile yaz
            return (deger & 0xFE) | bit;
        }

        // byte'ın bit sırasını tersine çevir
        // bu olmadan okuma yanlış çıkıyor (denedim, hata aldım)
        private static byte ByteTersCevir(byte b)
        {
            byte sonuc = 0;
            for (int i = 0; i < 8; i++)
            {
                sonuc = (byte)((sonuc << 1) | (b & 1));
                b >>= 1;
            }
            return sonuc;
        }
    }
}
