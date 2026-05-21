# Setup Interaksi Poster - VR Healthy

## Fitur
✅ Deteksi player mendekat ke poster  
✅ Tekan **E** untuk melihat gambar poster dalam pop-up  
✅ Movement player dikunci saat melihat poster  
✅ Tekan **Q** untuk menutup poster dan lanjut berjalan  
✅ Otomatis tutup jika player pergi jauh dari poster  

---

## Setup untuk Setiap Poster

### 1. **Pilih Poster GameObject di Scene**
   - Di Hierarchy, klik salah satu poster

### 2. **Tambahkan Collider Trigger**
   - Di Inspector → Add Component → Collider
   - Pilih **Box Collider** atau **Sphere Collider** (sesuai bentuk poster)
   - ✅ Centang `Is Trigger` (penting!)
   - Atur ukuran collider agar mencakup area sekitar poster (jarak interaksi ~3 meter)

### 3. **Tambahkan Script InteractionPoster**
   - Add Component → Search "InteractionPoster"
   - Pilih `InteractionPoster` script

### 4. **Setup di Inspector**
   ```
   Poster Settings:
   - Poster Image: Drag texture/image dari folder Assets/Poster/
   - Poster Title: Nama poster (contoh: "Masalah Kesehatan Reproduksi Wanita")
   
   Interaksi:
   - Interaction Distance: 3 (default, jarak player bisa trigger interaksi)
   ```

### 5. **Pastikan Player Punya Tag "Player"**
   - Select Player GameObject
   - Di Inspector → Tag → "Player" (buat jika belum ada)
   - Atau script akan auto-detect via VRWalkController component

---

## Cara Kerja Sistem

| Aksi | Hasil |
|------|-------|
| Player mendekat ke poster (jarak < 3m) | Siap untuk interaksi |
| Tekan **E** | Tampil pop-up gambar, movement terkunci |
| Tekan **Q** | Pop-up ditutup, movement normal kembali |
| Pergi jauh dari poster | Auto-tutup jika sedang dibuka |

---

## File yang Dimodifikasi

1. **InteractionPoster.cs** - Script interaksi poster (baru)
2. **VRWalkController.cs** - Ditambah method `LockMovement()` dan `UnlockMovement()`

---

## Tips

- Atur `Interaction Distance` lebih besar untuk area yang luas
- Gunakan `Box Collider` untuk poster dinding (lebih akurat)
- Gunakan `Sphere Collider` untuk interaksi area lingkaran
- Cek Console jika ada error saat mendekat poster (Tab Windows → General → Console)

---

## Contoh Struktur Poster di Scene

```
Poster (GameObject)
├── Mesh Collider (visual poster)
├── Box Collider (Trigger) ✓ Is Trigger
└── Script: InteractionPoster
    ├── Poster Image: [texture]
    ├── Poster Title: "Judul Poster"
    └── Interaction Distance: 3
```

---

**Good luck! 🎮**
