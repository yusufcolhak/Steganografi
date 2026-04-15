# Steganografi Uygulaması 

Bu proje, C#  kullanılarak geliştirilmiş bir **Steganografi (Veri Gizleme)** uygulamasıdır. Kullanıcıların herhangi bir resim dosyasının (.png, .bmp vb.) içerisine gizli metin mesajları gömmesine (saklamasına) ve daha sonra bu resimlerden gizli mesajları tekrar okuyup çıkarmasına olanak tanır.

##  Özellikler

*   **Metin Gizleme (Şifreleme Destekli):** İstediğiniz metin mesajını AES-256 algoritmasıyla şifreleyerek bir resim dosyasının pikselleri arasına gizler.
*   **Gizli Mesajı Çıkarma:** İçerisinde gizli veri bulunan bir resimden mesajı okuyup şifresini çözerek kullanıcıya gösterir.
*   **LSB (Least Significant Bit) Agoritması:** Resimlerin görsel kalitesini bozmadan, renk piksellerinin (RGB) en anlamsız bitlerini değiştirerek veri saklama işlemi gerçekleştirilir.
*   **AES-256 Şifreleme:** Gizlenen veriler resim dosyasının içerisinden dışarı sızsa bile, şifre anahtarı (parola) olmadan okunamaz.
*   **Kullanıcı Dostu Arayüz:** Basit ve sade WinForms tasarımı ile kolay kullanım sağlar.

##  Kullanılan Teknolojiler

*   **Dil:** C#
*   **Platform:** Windows Forms (.NET Framework)
*   **Şifreleme:** AES (Advanced Encryption Standard)
*   **Algoritma:** LSB (Least Significant Bit) Yöntemi

##  Nasıl Çalışır?

1.  **Resim Seç:** Üzerinde işlem yapmak istediğiniz hedef resmi seçin.
2.  **Mesajı Yaz:** Gizlemek istediğiniz metni ve ekstra güvenlik için bir parola belirleyin.
3.  **Gizle:** `Mesajı Gizle` butonuna basarak yeni ve içinde veri saklayan resmi kaydedin. (Kaydettiğiniz yeni resim çıplak gözle orijinalinden farksız görünecektir.)
4.  **Çıkar:** İçinde mesaj olan nesneyi seçin, belirlediğiniz şifreyi girin ve `Mesajı Çıkar` diyerek gizli veriye ulaşın.

##  Notlar
Bu proje, veri güvenliği ve steganografi (bilgi gizleme be bilimi) konseptlerini C# üzerinde pratik etmek amacıyla geliştirilmiş bir eğitim/ödev projesidir. Temel programlama ve hata yönetimi pratikleri anlaşılır biçimde kodlara yorum satırları ile eklenmiştir.
