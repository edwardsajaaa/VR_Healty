# Setup Interaksi Poster - VR Healthy

## Fitur
✅ Deteksi player mendekat ke poster  
✅ **Tap layar** untuk melihat poster dalam pop-up profesional (dark theme)  
✅ **Gallery View** — tampilan grid thumbnail poster (maks 3 per baris, scroll vertikal)  
✅ **Detail View** — tap thumbnail untuk melihat poster dalam ukuran besar  
✅ **Tombol "← Kembali"** — kembali ke gallery dari detail view  
✅ **Scroll vertikal** — untuk poster yang banyak (>3), bisa scroll ke bawah  
✅ Movement player dikunci saat melihat poster  
✅ Tekan **tombol Tutup** (✕) untuk menutup poster dan lanjut berjalan  
✅ Otomatis tutup jika player pergi jauh dari poster  
✅ UI lebih dekat ke player (0.8m) untuk kenyamanan membaca di VR  
✅ Kompatibel dengan HP Android (touch screen)  

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
   - Poster Images: Drag semua texture/gambar poster ke array ini
     (bisa 1 gambar atau lebih, misal 3, 5, atau 8 gambar)
   - Poster Title: Nama poster (contoh: "Masalah Kesehatan Reproduksi Wanita")
   
   Interaksi:
   - Interaction Distance: 3 (default, jarak player bisa trigger interaksi)
   - Canvas Distance: 0.8 (jarak UI dari kamera, semakin kecil semakin dekat)
   - Canvas Scale: 0.001 (ukuran UI, semakin besar semakin besar)
   
   Warna UI (Dark Theme):
   - Panel Color: Warna background panel utama
   - Header Color: Warna background header
   - Accent Color: Warna garis aksen & badge nomor
   - Card Color: Warna background thumbnail card
   (semua warna bisa dikustomisasi dari Inspector)
   ```

### 5. **Pastikan Player Punya Tag "Player"**
   - Select Player GameObject
   - Di Inspector → Tag → "Player" (buat jika belum ada)
   - Atau script akan auto-detect via VRWalkController component

---

## Cara Kerja Sistem

### Flow Interaksi

```
Player mendekat → Hint muncul → Tap layar → Gallery View
                                                │
                                    ┌───────────┼───────────┐
                                    ▼           ▼           ▼
                              [Poster 1]  [Poster 2]  [Poster 3]
                                    │                       
                              Tap poster                    
                                    ▼                       
                              Detail View (fullscreen)      
                                    │                       
                            ← Kembali (ke gallery)          
                            ✕ Tutup (keluar semua)          
```

### Tabel Aksi

| Aksi | Hasil |
|------|-------|
| Player mendekat ke poster (jarak < 3m) | Hint "Ketuk layar..." muncul |
| **Tap layar** (atau klik kiri di Editor) | Tampil **Gallery View** (grid thumbnail) |
| **Tap thumbnail poster** | Masuk **Detail View** (poster besar) |
| Tekan **← Kembali** | Kembali ke Gallery View |
| Tekan **✕ Tutup** (header/bawah) | Tutup seluruh poster UI |
| Pergi jauh dari poster | Auto-tutup |

### Kasus Khusus

| Kondisi | Perilaku |
|---------|----------|
| Hanya **1 gambar** | Langsung ke Detail View (skip gallery) |
| **≤ 3 gambar** | 1 baris, tanpa scroll |
| **> 3 gambar** (misal 8) | Grid 3 kolom, scroll vertikal ke bawah |

---

## File yang Terlibat

1. **InteractionPoster.cs** - Script interaksi poster (Gallery + Detail View)
2. **VRWalkController.cs** - Method `LockMovement()` dan `UnlockMovement()`

---

## Tips

- Atur `Canvas Distance` lebih kecil (misal 0.6) jika UI masih terasa jauh
- Atur `Canvas Scale` lebih besar (misal 0.0012) jika UI masih terasa kecil
- Gunakan `Box Collider` untuk poster dinding (lebih akurat)
- Gunakan `Sphere Collider` untuk interaksi area lingkaran
- Warna UI bisa dikustomisasi sepenuhnya dari Inspector (Dark Theme default)
- Cek Console jika ada error (Tab Windows → General → Console)

---

## Contoh Struktur Poster di Scene

```
Poster (GameObject)
├── Mesh Collider (visual poster)
├── Box Collider (Trigger) ✓ Is Trigger
└── Script: InteractionPoster
    ├── Poster Images: [gambar1, gambar2, gambar3, ...]
    ├── Poster Title: "Judul Poster"
    ├── Canvas Distance: 0.8
    ├── Canvas Scale: 0.001
    └── Interaction Distance: 3
```

---

**Good luck! 🎮**
